/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Allows you to specify assertions on child nodes of an instance. A different set of assertions can be applied to a child, depending
    /// on its name.
    /// </summary>
    [DataContract]
    public class ChildrenValidator : IMergeable, IValidatable, IReadOnlyDictionary<string, IAssertion>
    {
        private readonly Dictionary<string, IAssertion> _childList = new();

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public IReadOnlyDictionary<string, IAssertion> ChildList { get => _childList; }

        [DataMember(Order = 1)]
                public bool AllowAdditionalChildren { get; init; }
#else
        [DataMember]
        public IReadOnlyDictionary<string, IAssertion> ChildList { get => _childList; }

        [DataMember]
        public bool AllowAdditionalChildren { get; init; }
#endif

        public ChildrenValidator() : this(false)
        {

        }

        public ChildrenValidator(bool allowAdditionalChildren, params (string name, IAssertion assertion)[] childList) :
            this(childList, allowAdditionalChildren)
        {
        }

        public ChildrenValidator(IEnumerable<KeyValuePair<string, IAssertion>> childList, bool allowAdditionalChildren = false)
        {
            _childList = childList is Dictionary<string, IAssertion> dict ? dict : new Dictionary<string, IAssertion>(childList);
            AllowAdditionalChildren = allowAdditionalChildren;
        }

        public ChildrenValidator(IEnumerable<(string name, IAssertion assertion)> childList, bool allowAdditionalChildren = false) :
            this(childList.ToDictionary(p => p.name, p => p.assertion), allowAdditionalChildren)
        {
        }

        public IAssertion? Lookup(string name) =>
            ChildList.TryGetValue(name, out var child) ? child : null;

        public IMergeable Merge(IMergeable other)
        {
            if (other is ChildrenValidator cd)
            {
                var mergedChildren = from name in names()
                                     let left = this.Lookup(name)
                                     let right = cd.Lookup(name)
                                     select (name, merge(left, right));
                return new ChildrenValidator(mergedChildren);
            }
            else
                throw Error.InvalidOperation($"Internal logic failed: tried to merge Children with an {other.GetType().Name}");

            IEnumerable<string> names() => ChildList.Keys.Union(cd.ChildList.Keys).Distinct();

            IAssertion merge(IAssertion l, IAssertion r)
                => l is null ? r : r is null ? l : new ElementSchema(l, r);
        }

        public JToken ToJson() =>
            new JProperty("children", new JObject() { ChildList.Select(child =>
                new JProperty(child.Key, child.Value.ToJson().MakeNestedProp())) });

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var element = input.AddValueNode();

            var result = Assertions.EMPTY;

            if (element.Value is null && !element.Children().Any())
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_ELEMENT_MUST_HAVE_VALUE_OR_CHILDREN, element.Location, "Element must not be empty"));
            }

            var matchResult = ChildNameMatcher.Match(ChildList, element);
            if (matchResult.UnmatchedInstanceElements.Any() && !AllowAdditionalChildren)
            {
                var elementList = String.Join(",", matchResult.UnmatchedInstanceElements.Select(e => $"'{e.Name}'"));
                result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN, input.Location, $"Encountered unknown child elements {elementList} for definition '{"TODO: definition.Path"}'"));
            }

            result += await matchResult.Matches.Select(m => m.Assertion.Validate(m.InstanceElements, vc, state)).AggregateAssertions();
            return result;
        }

        public bool ContainsKey(string key) => _childList.ContainsKey(key);
        public bool TryGetValue(string key, out IAssertion value) => _childList.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, IAssertion>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, IAssertion>>)_childList).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_childList).GetEnumerator();

        public IEnumerable<string> Keys => _childList.Keys;

        public IEnumerable<IAssertion> Values => _childList.Values;

        public int Count => _childList.Count;
        public IAssertion this[string key]
        {
            get => _childList[key];
            init => _childList[key] = value;
        }
    }

    internal class ChildNameMatcher
    {
        public static MatchResult Match(IReadOnlyDictionary<string, IAssertion> assertions, ITypedElement instanceParent)
        {
            var elementsToMatch = instanceParent.Children().ToList();

            List<Match> matches = new();

            foreach (var assertion in assertions)
            {
                var match = new Match(assertion.Value, new List<ITypedElement>());

                // Special case is the .value of a primitive fhir type, this is represented
                // as the "Value" of the IValueProvider interface, not as a real child
                //if (definitionElement.Current.IsPrimitiveValueConstraint())
                //{
                //    if (instanceParent.Value != null)
                //        match.InstanceElements.Add(instanceParent);
                //}
                //else
                //{
                var found = elementsToMatch.Where(ie => nameMatches(assertion.Key, ie)).ToList();

                // Note that if *no* children are found matching this child assertion, this is still considered
                // a match: there are simply 0 children for this item. This ensures that cardinality constraints
                // can be propertly enforced, even on empty sets.
                match.InstanceElements.AddRange(found);
                elementsToMatch.RemoveAll(e => found.Contains(e));

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
        public IAssertion Assertion;
        public List<ITypedElement> InstanceElements;

        public Match(IAssertion assertion, List<ITypedElement> instanceElements)
        {
            Assertion = assertion;
            InstanceElements = instanceElements;
        }
    }
}
