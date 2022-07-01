/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Allows you to specify assertions on child nodes of an instance. A different set of assertions 
    /// can be applied to a child, depending on its name.
    /// </summary>
    [DataContract]
    public class ChildrenValidator : IValidatable, IReadOnlyDictionary<string, ChildConstraints>
    {
        private readonly Dictionary<string, ChildConstraints> _childList = new();

        /// <summary>
        /// The list of children that this validator needs to validate.
        /// </summary>
        [DataMember]
        public IReadOnlyDictionary<string, ChildConstraints> ChildList => _childList;

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

        /// <inheritdoc cref="ChildrenValidator(IEnumerable{ChildConstraints}, bool)"/>
        public ChildrenValidator(bool allowAdditionalChildren, params (string name, IAssertion assertion)[] childList) :
            this(childList.Select(c => new ChildConstraints(c.name, null, c.assertion)), allowAdditionalChildren)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildrenValidator"/> class given
        /// a list of children an indication of whether to allow additional children.
        /// </summary>
        public ChildrenValidator(IEnumerable<ChildConstraints> childList, bool allowAdditionalChildren = false)
        {
            _childList = childList.ToDictionary(cc => cc.Name);
            AllowAdditionalChildren = allowAdditionalChildren;
        }

        /// <summary>
        /// Tries to find a child by name within the <see cref="ChildList"/>. 
        /// </summary>
        /// <returns>The child if found, <c>null</c> otherwise.</returns>
        public ChildConstraints? Lookup(string name) =>
            ChildList.TryGetValue(name, out var child) ? child : null;

        /// <inheritdoc />
        public JToken ToJson()
        {
            return new JProperty("children",
                new JObject(
                    ChildList.Select((System.Func<KeyValuePair<string, ChildConstraints>, JProperty>)(child =>
                    {
                        var cardValidator = child.Value.Cardinality ?? new CardinalityValidator();
                        var card = ((JProperty)cardValidator.ToJson()).Value.ToString();
                        return new JProperty(child.Key + $" [{card}]", JsonExtensions.MakeNestedProp(child.Value.Assertions.ToJson()));
                    }))
                ));
        }

        /// <inheritdoc />
        public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            // Listing children can be an expensive operation, so make sure we run it once.
            var elementsToMatch = input.Children().ToList();

            // TODO: This is actually ele-1, and we should replace that FP validator with
            // this single statement in its place.
            bool hasNoElementsButId = elementsToMatch.Count == 0; // || (elementsToMatch.Count == 1 && elementsToMatch[0].Name == "id");
            if (input.Value is null && hasNoElementsButId)
                return ResultAssertion.FromEvidence(new IssueAssertion(Issue.CONTENT_ELEMENT_MUST_HAVE_VALUE_OR_CHILDREN, input.Location, "Element must not be empty"));

            // If this is a node with a primitive value, simulate having a child with
            // this value and the corresponding System type as an ITypedElement
            if (input.Value is not null && char.IsLower(input.InstanceType[0]) && !elementsToMatch.Any())
                elementsToMatch.Insert(0, new ValueElementNode(input));

            var evidence = new List<IResultAssertion>();
            var matchResult = ChildNameMatcher.Match(ChildList, elementsToMatch);
            if (matchResult.UnmatchedInstanceElements.Any() && !AllowAdditionalChildren)
            {
                var elementList = string.Join(",", matchResult.UnmatchedInstanceElements.Select(e => $"'{e.Name}'"));
                evidence.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN, input.Location, $"Encountered unknown child elements {elementList} for definition '{"TODO: definition.Path"}'"));
            }

            evidence.AddRange(matchResult.Matches.SelectMany(m => validateChild(m)));
            return ResultAssertion.FromEvidence(evidence);

            IEnumerable<ResultAssertion> validateChild(Match m)
            {
                var location = input.Location + "." + m.ChildName;
                if (m.Constraints.Cardinality is not null)
                    yield return m.Constraints.Cardinality.ValidateMany(m.InstanceElements ?? NO_CHILDREN, location, vc, state);

                if (m.InstanceElements is not null)
                    yield return m.Constraints.Assertions.ValidateMany(m.InstanceElements, location, vc, state);
            }
        }

        private readonly IEnumerable<ITypedElement> NO_CHILDREN = Enumerable.Empty<ITypedElement>();

        #region IDictionary implementation
        /// <inheritdoc />
        public bool ContainsKey(string key) => _childList.ContainsKey(key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out ChildConstraints value) => _childList.TryGetValue(key, out value);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, ChildConstraints>> GetEnumerator() =>
            ((IEnumerable<KeyValuePair<string, ChildConstraints>>)_childList).GetEnumerator();

        /// <inheritdoc />
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_childList).GetEnumerator();

        /// <inheritdoc />
        public IEnumerable<string> Keys => _childList.Keys;

        /// <inheritdoc />
        public IEnumerable<ChildConstraints> Values => _childList.Values;

        /// <inheritdoc />
        public int Count => _childList.Count;

        /// <inheritdoc />
        public ChildConstraints this[string key]
        {
            get => _childList[key];
            init => _childList[key] = value;
        }
        #endregion
    }

    /// <summary>
    /// Specifies the constraints for children with a given name.
    /// </summary>
    [DataContract]
    public class ChildConstraints
    {
        /// The name of the children this constraint is about.
        [DataMember]
        public string Name { get; init; }

        /// The cardinality to count the number of children against.
        [DataMember]
        public CardinalityValidator? Cardinality { get; init; }

        /// Additional assertions to validate the children against.
        [DataMember]
        public IAssertion Assertions { get; init; }

        /// <summary>
        /// Constructs a new ChildConstraints.
        /// </summary>
        public ChildConstraints(string name, CardinalityValidator? cardinality, IAssertion assertions)
        {
            Name = name;
            Cardinality = cardinality;
            Assertions = assertions;
        }
    }


    internal class ChildNameMatcher
    {
        public static MatchResult Match(IReadOnlyDictionary<string, ChildConstraints> constraints, IEnumerable<ITypedElement> children)
        {
            var elementsToMatch = children.ToList();

            List<Match> matches = new();

            foreach (var constraint in constraints)
            {
                var match = new Match(constraint.Key, constraint.Value);
                var found = elementsToMatch.Where(ie => nameMatches(constraint.Key, ie)).ToList();

                // Note that if *no* children are found matching this child assertion, this is still considered
                // a match: there are simply 0 children for this item (list of children is null in this case).
                // This ensures that cardinality constraints can be propertly enforced, even on empty sets.
                if (found.Count > 0)
                {
                    match.InstanceElements ??= new();
                    match.InstanceElements.AddRange(found);
                    foreach (var e in found) elementsToMatch.Remove(e);
                }

                matches.Add(match);
            }

            MatchResult result = new()
            {
                Matches = matches,
                UnmatchedInstanceElements = elementsToMatch
            };

            return result;
        }

        private static bool nameMatches(string name, ITypedElement instanceElement)
        {
            var definedName = name;

            // simple direct match
            if (definedName == instanceElement.Name) return true;

            // match where definition path includes a type suffix (typeslice shorthand)
            // example: path Patient.deceasedBoolean matches Patient.deceased (with type 'boolean')
            if (definedName == instanceElement.Name + instanceElement.InstanceType.Capitalize()) return true;

            // match where definition path is a choice (suffix '[x]'), in this case
            // match the path without the suffix against the name
            if (definedName.EndsWith("[x]"))
            {
                if (definedName[0..^3] == instanceElement.Name) return true;
            }

            return false;
        }
    }

    /// <summary>
    /// The result of matching children in an instance against the (expected) children in the definition of the type.
    /// </summary>
    internal class MatchResult
    {
        /// <summary>
        /// The list of children that matched an element in the definition of the type.
        /// </summary>
        public List<Match>? Matches;

        /// <summary>
        /// The list of children that could not be matched against the defined list of children in the definition
        /// of the type.
        /// </summary>
        public List<ITypedElement>? UnmatchedInstanceElements;
    }

    /// <summary>
    /// This is a pair that corresponds to a set of elements that needs to be validated against an assertions.
    /// </summary>
    /// <remarks>Usually, this is the set of elements with the same name and the group of assertions that represents
    /// the validation rule for that element generated from the StructureDefinition.</remarks>
    internal class Match
    {
        public string ChildName;
        public ChildConstraints Constraints;
        public List<ITypedElement>? InstanceElements;

        public Match(string childName, ChildConstraints assertion)
        {
            ChildName = childName;
            Constraints = assertion;
            InstanceElements = null;
        }
    }
}
