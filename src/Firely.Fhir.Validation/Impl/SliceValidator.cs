﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Enables defining subsets (called slices) and conditions on those subsets of repeats of an element.
    /// </summary>
    /// <remarks>This functionality works like a "switch" statement, where each repeat is classified into a
    /// case based on a condition (called the discriminator). Each of the instances for a given case can then
    /// be validated against the assertions defined for each case.
    /// </remarks>
    [DataContract]
    public class SliceValidator : IGroupValidatable
    {
        /// <summary>
        /// Represents a named, conditional assertion on a set of elements.
        /// </summary>
        /// <remarks>This class is used to encode the discriminator (as <see cref="Condition"/>) and the sub-constraints
        /// for the slice (as <see cref="Assertion"/>).</remarks>
        [DataContract]
        public class SliceCase
        {
            /// <summary>
            /// Name of the slice. Used for diagnostic purposes.
            /// </summary>
            [DataMember]
            public string Name { get; private set; }

            /// <summary>
            /// Condition an instance must satisfy to match this slice.
            /// </summary>
            [DataMember]
            public IAssertion Condition { get; private set; }

            /// <summary>
            /// Assertion that all instances for this slice must be validated against.
            /// </summary>
            [DataMember]
            public IAssertion Assertion { get; private set; }

            /// <summary>
            /// Construct a single <see cref="SliceCase"/> in a <see cref="SliceValidator"/>.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="condition"></param>
            /// <param name="assertion"></param>
            public SliceCase(string name, IAssertion condition, IAssertion assertion)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Condition = condition ?? throw new ArgumentNullException(nameof(condition));
                Assertion = assertion ?? throw new ArgumentNullException(nameof(assertion));
            }

            /// <inheritdoc cref="IJsonSerializable.ToJson"/>
            public JToken ToJson() =>
                new JObject(
                    new JProperty("name", Name),
                    new JProperty("condition", Condition.ToJson().MakeNestedProp()),
                    new JProperty("assertion", Assertion.ToJson().MakeNestedProp())
                    );
        }

        /// <summary>
        /// Determines whether the instances in this group must appear in the same order as the slices.
        /// </summary>
        [DataMember]
        public bool Ordered { get; private set; }

        /// <summary>
        /// Determines whether all instances that do not match a slice must appear at the end.
        /// </summary>
        [DataMember]
        public bool DefaultAtEnd { get; private set; }

        /// <summary>
        /// An assertion that will be used to validate all instances not matching a slice.
        /// </summary>
        [DataMember]
        public IAssertion Default { get; private set; }

        /// <summary>
        /// Defined slices for this slice group.
        /// </summary>
        [DataMember]
        public IReadOnlyList<SliceCase> Slices { get; private set; }

        /// <summary>
        /// Constuct a slice group.
        /// </summary>
        public SliceValidator(bool ordered, bool defaultAtEnd, IAssertion @default, params SliceCase[] slices) : this(ordered, defaultAtEnd, @default, slices.AsEnumerable())
        {
        }

        /// <inheritdoc cref="SliceValidator.SliceValidator(bool, bool, IAssertion, SliceCase[])"/>
        public SliceValidator(bool ordered, bool defaultAtEnd, IAssertion @default, IEnumerable<SliceCase> slices)
        {
            Ordered = ordered;
            DefaultAtEnd = defaultAtEnd;
            Default = @default ?? throw new ArgumentNullException(nameof(@default));
            Slices = slices.ToArray() ?? throw new ArgumentNullException(nameof(slices));
        }


        public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState state) => Validate(new[] { input }, input.Location, vc, state);

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, string, ValidationContext, ValidationState)"/>
        public ResultAssertion Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var lastMatchingSlice = -1;
            var defaultInUse = false;
            List<IAssertion> evidence = new();
            var buckets = new Buckets(Slices, Default, groupLocation);

            var candidateNumber = 0;  // instead of location - replace this with location later.
            var traces = new List<TraceAssertion>();

            // Go over the elements in the instance, in order
            foreach (var candidate in input)
            {
                candidateNumber += 1;
                bool hasSucceeded = false;

                // Try to find the child slice that this element matches
                for (var sliceNumber = 0; sliceNumber < Slices.Count; sliceNumber++)
                {
                    var sliceName = Slices[sliceNumber].Name;
                    var conditionResult = Slices[sliceNumber].Condition.ValidateOne(candidate, vc, state);

                    if (conditionResult.IsSuccessful)
                    {
                        // traces.Add(new TraceAssertion(groupLocation, $"Input[{candidateNumber}] matched slice {sliceName}."));

                        //TODO: If the bucket is *not* group validatable we might as well immediately
                        //validate the hit against the bucket - if it fails we can bail out early.
                        //A simpler case of this more generic case is when the bucket is a constant
                        //ResultAssertion with result failure. This may save quite a lot of processing time.

                        // The instance matched a slice that we have already passed, if order matters, 
                        // this is not allowed
                        if (sliceNumber < lastMatchingSlice && Ordered)
                            evidence.Add(ResultAssertion.FromEvidence(
                                new IssueAssertion(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER, groupLocation, $"Element matches slice '{sliceName}', but this is out of order for this group, since a previous element already matched slice '{Slices[lastMatchingSlice].Name}'")));
                        else
                            lastMatchingSlice = sliceNumber;

                        if (defaultInUse && DefaultAtEnd)
                        {
                            // We found a match while we already added a non-match to a "open at end" slicegroup, that's not allowed
                            evidence.Add(ResultAssertion.FromEvidence(
                                new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, groupLocation, $"Element matched slice '{sliceName}', but it appears after a non-match, which is not allowed for an open-at-end group")));
                        }

                        hasSucceeded = true;
                        //result += conditionResult; - for discriminatorless slicing, this would actually be "the" result, but
                        //we don't know we're using discriminatorless slicing here anymore.  For all other slicing, it is not
                        //necessary to know why we failed each individual case (except maybe for debugging purposes, but this would
                        //produce an enormous amount of information

                        // to add to slice
                        buckets.AddToSlice(Slices[sliceNumber], candidate);
                        break;
                    }
                }

                // So we found no slice that can take this candidate, let's pass it to the default slice
                if (!hasSucceeded)
                {
                    // traces.Add(new TraceAssertion(groupLocation, $"Input[{candidateNumber}] did not match any slice."));

                    defaultInUse = true;
                    buckets.AddToDefault(candidate);
                }
            }

            var bucketAssertions = buckets.Validate(vc, state);

            return ResultAssertion.FromEvidence(
                    evidence.Concat(traces).Concat(bucketAssertions));
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var def = Default.ToJson();
            if (def is JProperty) def = new JObject(def);

            return new JProperty("slice", new JObject(
                new JProperty("ordered", Ordered),
                new JProperty("defaultAtEnd", DefaultAtEnd),
                new JProperty("case", new JArray() { Slices.Select(s => s.ToJson()) }),
                new JProperty("default", def)));
        }

        private class Buckets : Dictionary<SliceCase, IList<ITypedElement>?>
        {
            private readonly List<ITypedElement> _defaultBucket = new();
            private readonly IAssertion _defaultAssertion;
            private readonly string _groupLocation;

            public Buckets(IEnumerable<SliceCase> slices, IAssertion defaultAssertion, string groupLocation)
            {
                // initialize the buckets according to the slice definitions
                foreach (var item in slices)
                {
                    this.Add(item, null);
                }

                _defaultAssertion = defaultAssertion;
                _groupLocation = groupLocation;
            }

            public void AddToSlice(SliceCase slice, ITypedElement item)
            {
                if (!TryGetValue(slice, out var list))
                    throw new InvalidOperationException($"Slice should have been initialized with item {slice.Name}.");

                if (list is null)
                    list = this[slice] = new List<ITypedElement>();

                list.Add(item);
            }

            public void AddToDefault(ITypedElement item) => _defaultBucket.Add(item);

            public ResultAssertion[] Validate(ValidationContext vc, ValidationState state)
                => this.Select(slice => slice.Key.Assertion.ValidateMany(slice.Value ?? NOELEMENTS, _groupLocation, vc, state))
                        .Append(_defaultAssertion.ValidateMany(_defaultBucket, _groupLocation, vc, state)).ToArray();

            private static readonly List<ITypedElement> NOELEMENTS = new();
        }
    }

    /*
     * 
     
   "slice-discrimatorless": {
   "ordered": false,
	"case": [
	  {
		"name": "case-1"
		"condition": { "maxValue": 30 },
		"assertion": {
			"$id": "#slicename",
			"ele-1": "hasValue() or (children().count() > id.count())",
			"ext-1": "extension.exists() != value.exists()",
			"max": 1
		}
	  }
	]
}

"slice-value": {
   "ordered": false,
	"case": [
	  {
		"name": "phone"
		"condition": { 
			"fpath": "system" ,
			"fixed": "phone"
		}
		"assertion": {
			"$id": "#slicename",
            "min": 1
		}
	  },
	  {
		"name": "email"
		"condition": { 
			"fpath": "system" ,
			"fixed": "email"
		}
		"assertion": {
			"$id": "#slicename",
            "min": 1
		}
	  },
	  
	]
}
*/


}

