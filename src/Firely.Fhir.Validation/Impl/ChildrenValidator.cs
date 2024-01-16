/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Allows you to specify assertions on child nodes of an instance. A different set of assertions 
    /// can be applied to a child, depending on its name.
    /// </summary>
    [DataContract]
    internal class ChildrenValidator : IValidatable, IReadOnlyDictionary<string, IAssertion>
    {
        private readonly Dictionary<string, IAssertion> _childList = new();

        /// <summary>
        /// The list of children that this validator needs to validate.
        /// </summary>
        [DataMember]
        public IReadOnlyDictionary<string, IAssertion> ChildList => _childList;

        /// <summary>
        /// Whether it is valid for an instance to have children not present in the
        /// <see cref="ChildList"/>.
        /// </summary>
        [DataMember]
        public bool AllowAdditionalChildren { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildrenValidator"/> class.
        /// </summary>
        public ChildrenValidator() : this(false)
        {
        }

        /// <inheritdoc cref="ChildrenValidator(IEnumerable{KeyValuePair{string, IAssertion}}, bool)"/>
        public ChildrenValidator(bool allowAdditionalChildren, params (string name, IAssertion assertion)[] childList) :
            this(childList, allowAdditionalChildren)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildrenValidator"/> class given
        /// a list of children an indication of whether to allow additional children.
        /// </summary>
        public ChildrenValidator(IEnumerable<KeyValuePair<string, IAssertion>> childList, bool allowAdditionalChildren = false)
        {
            _childList = childList is Dictionary<string, IAssertion> dict ? dict : new Dictionary<string, IAssertion>(childList);
            AllowAdditionalChildren = allowAdditionalChildren;
        }

        /// <inheritdoc cref="ChildrenValidator(IEnumerable{KeyValuePair{string, IAssertion}}, bool)"/>
        public ChildrenValidator(IEnumerable<(string name, IAssertion assertion)> childList, bool allowAdditionalChildren = false) :
            this(childList.ToDictionary(p => p.name, p => p.assertion), allowAdditionalChildren)
        { }

        /// <summary>
        /// Tries to find a child by name within the <see cref="ChildList"/>. 
        /// </summary>
        /// <returns>The child if found, <c>null</c> otherwise.</returns>
        public IAssertion? Lookup(string name) =>
            ChildList.TryGetValue(name, out var child) ? child : null;

        /// <inheritdoc />
        public JToken ToJson() =>
            new JProperty("children", new JObject() { ChildList.Select(child =>
                new JProperty(child.Key, child.Value.ToJson().MakeNestedProp())) });

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            var evidence = new List<ResultReport>();

            // Listing children can be an expensive operation, so make sure we run it once.
            var elementsToMatch = input.Children().ToList();

            // If this is a node with a primitive value, simulate having a child with
            // this value and the corresponding System type as an ITypedElement
            if (input.Value is not null && char.IsLower(input.InstanceType[0]) && !elementsToMatch.Any())
                elementsToMatch.Insert(0, new ValueElementNode(input));

            var matchResult = ChildNameMatcher.Match(ChildList, elementsToMatch);
            if (matchResult.UnmatchedInstanceElements?.Count > 0 && !AllowAdditionalChildren)
            {
                var elementList = string.Join(",", matchResult.UnmatchedInstanceElements.Select(e => $"'{e.Name}'"));
                evidence.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN, $"Encountered unknown child elements {elementList}")
                    .AsResult(state));
            }

            evidence.AddRange(
                matchResult.Matches?.Select(m =>
                    m.Assertion.ValidateMany(
                        m.InstanceElements ?? NOELEMENTS,
                        vc,
                        state
                            .UpdateLocation(vs => vs.ToChild(m.ChildName))
                            .UpdateInstanceLocation(ip => ip.ToChild(m.ChildName, choiceElement(m)))
                    )) ?? Enumerable.Empty<ResultReport>());

            return ResultReport.Combine(evidence);

            static string? choiceElement(Match m) => m.ChildName.EndsWith("[x]") ? m.InstanceElements?.FirstOrDefault()?.InstanceType : null;
        }

        private static readonly List<IScopedNode> NOELEMENTS = new();

        #region IDictionary implementation
        /// <inheritdoc />
        public bool ContainsKey(string key) => _childList.ContainsKey(key);

#if NET8_0
        /// <inheritdoc />
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IAssertion value) => _childList.TryGetValue(key, out value);
#else
        /// <inheritdoc />
        public bool TryGetValue(string key, out IAssertion value) => _childList.TryGetValue(key, out value);
#endif 

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, IAssertion>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, IAssertion>>)_childList).GetEnumerator();

        /// <inheritdoc />
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_childList).GetEnumerator();

        /// <inheritdoc />
        public IEnumerable<string> Keys => _childList.Keys;

        /// <inheritdoc />
        public IEnumerable<IAssertion> Values => _childList.Values;

        /// <inheritdoc />
        public int Count => _childList.Count;

        /// <inheritdoc />
        public IAssertion this[string key]
        {
            get => _childList[key];
            init => _childList[key] = value;
        }
        #endregion
    }

    internal class ChildNameMatcher
    {
        public static MatchResult Match(IReadOnlyDictionary<string, IAssertion> assertions, IEnumerable<IScopedNode> children)
        {
            var elementsToMatch = children.ToList();

            List<Match> matches = new();

            foreach (var assertion in assertions)
            {

                var found = elementsToMatch.Where(ie => nameMatches(assertion.Key, ie)).ToList();

                // Note that if *no* children are found matching this child assertion, this is still considered
                // a match: there are simply 0 children for this item. This ensures that cardinality constraints
                // can be propertly enforced, even on empty sets.

                Match match = found.Any()
                    ? new(assertion.Key, assertion.Value, found)
                    : new(assertion.Key, assertion.Value, null);
                elementsToMatch.RemoveAll(e => found.Contains(e));

                matches.Add(match);
            }

            return new(matches, elementsToMatch.ToList());
        }

        private static bool nameMatches(string name, IScopedNode instanceElement)
        {
            var definedName = name;

            // simple direct match
            if (definedName == instanceElement.Name) return true;

            // match where definition path includes a type suffix (typeslice shorthand)
            // example: path Patient.deceasedBoolean matches Patient.deceased (with type 'boolean')
            if (definedName == instanceElement.Name + instanceElement.InstanceType.Capitalize()) return true;

            // match where definition path is a choice (suffix '[x]'), in this case
            // match the path without the suffix against the name
            if (definedName.EndsWith("[x]") && definedName[0..^3] == instanceElement.Name) return true;

            return false;
        }
    }

    /// <summary>
    ///  The result of matching children in an instance against the (expected) children in the definition of the type.
    /// </summary>
    /// <param name="Matches">The list of children that matched an element in the definition of the type.</param>
    /// <param name="UnmatchedInstanceElements">The list of children that could not be matched against the defined list of children in the definition
    /// of the type.</param>
    internal record MatchResult(List<Match>? Matches, List<IScopedNode>? UnmatchedInstanceElements);

    /// <summary>
    /// This is a pair that corresponds to a set of elements that needs to be validated against an assertion.
    /// </summary>
    /// <param name="ChildName">Name of the child element.</param>
    /// <param name="Assertion">The assertion to execute.</param>
    /// <param name="InstanceElements">Set of elements belong to this child</param>
    /// <remarks>Usually, this is the set of elements with the same name and the group of assertions that represents
    /// the validation rule for that element generated from the StructureDefinition.</remarks>
    internal record Match(string ChildName, IAssertion Assertion, List<IScopedNode>? InstanceElements = null);
}