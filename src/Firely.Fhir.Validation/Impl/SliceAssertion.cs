using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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
    public class SliceAssertion : IGroupValidatable
    {
        /// <summary>
        /// Represents a named, conditional assertion on a set of elements.
        /// </summary>
        /// <remarks>This class is used to encode the discriminator (as <see cref="Condition"/>) and the sub-constraints
        /// for the slice (as <see cref="Assertion"/>).</remarks>
        [DataContract]
        public class Slice : IAssertion
        {
#if MSGPACK_KEY
            /// <summary>
            /// Name of the slice. Used for diagnostic purposes.
            /// </summary>
            [DataMember(Order = 0)]
            public string Name { get; private set; }

            /// <summary>
            /// Condition an instance must satisfy to match this slice.
            /// </summary>
            [DataMember(Order = 1)]
            public IAssertion Condition { get; private set; }

            /// <summary>
            /// Assertion that all instances for this slice must be validated against.
            /// </summary>
            [DataMember(Order = 2)]
            public IAssertion Assertion { get; private set; }
#else
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
#endif

            /// <summary>
            /// Construct a single <see cref="Slice"/> in a <see cref="SliceAssertion"/>.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="condition"></param>
            /// <param name="assertion"></param>
            public Slice(string name, IAssertion condition, IAssertion assertion)
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

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public bool Ordered { get; private set; }

        [DataMember(Order = 1)]
        public bool DefaultAtEnd { get; private set; }

        [DataMember(Order = 2)]
        public IAssertion Default { get; private set; }

        [DataMember(Order = 3)]
        public Slice[] Slices { get; private set; }
#else
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
        public Slice[] Slices { get; private set; }
#endif

        /// <summary>
        /// Constuct a slice group.
        /// </summary>
        /// <param name="ordered"></param>
        /// <param name="defaultAtEnd"></param>
        /// <param name="default"></param>
        /// <param name="slices"></param>
        public SliceAssertion(bool ordered, bool defaultAtEnd, IAssertion @default, params Slice[] slices) : this(ordered, defaultAtEnd, @default, slices.AsEnumerable())
        {
        }

        /// <inheritdoc cref="SliceAssertion.SliceAssertion(bool, bool, IAssertion, Slice[])"/>
        public SliceAssertion(bool ordered, bool defaultAtEnd, IAssertion @default, IEnumerable<Slice> slices)
        {
            Ordered = ordered;
            DefaultAtEnd = defaultAtEnd;
            Default = @default ?? throw new ArgumentNullException(nameof(@default));
            Slices = slices.ToArray() ?? throw new ArgumentNullException(nameof(slices));
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, ValidationContext)"/>
        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var lastMatchingSlice = -1;
            var defaultInUse = false;
            Assertions result = Assertions.EMPTY;
            var buckets = new Buckets(Slices, Default);

            var candidateNumber = 0;  // instead of location - replace this with location later.
            var traces = new List<Trace>();

            // Go over the elements in the instance, in order
            foreach (var candidate in input)
            {
                candidateNumber += 1;
                bool hasSucceeded = false;

                // Try to find the child slice that this element matches
                for (var sliceNumber = 0; sliceNumber < Slices.Length; sliceNumber++)
                {
                    var sliceName = Slices[sliceNumber].Name;
                    var conditionResult = await Slices[sliceNumber].Condition.Validate(candidate, vc).ConfigureAwait(false);

                    if (conditionResult.Result.IsSuccessful)
                    {
                        traces.Add(new Trace($"Input[{candidateNumber}] matched slice {sliceName}."));

                        //TODO: If the bucket is *not* group validatable we might as well immediately
                        //validate the hit against the bucket - if it fails we can bail out early.
                        //A simpler case of this more generic case is when the bucket is a constant
                        //ResultAssertion with result failure. This may save quite a lot of processing time.

                        // The instance matched a slice that we have already passed, if order matters, 
                        // this is not allowed
                        if (sliceNumber < lastMatchingSlice && Ordered)
                            result += ResultAssertion.CreateFailure(
                                new IssueAssertion(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER, "TODO", $"Element matches slice '{sliceName}', but this is out of order for this group, since a previous element already matched slice '{Slices[lastMatchingSlice].Name}'"));
                        else
                            lastMatchingSlice = sliceNumber;

                        if (defaultInUse && DefaultAtEnd)
                        {
                            // We found a match while we already added a non-match to a "open at end" slicegroup, that's not allowed
                            result += ResultAssertion.CreateFailure(
                                new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO", $"Element matched slice '{sliceName}', but it appears after a non-match, which is not allowed for an open-at-end group"));
                        }

                        hasSucceeded = true;
                        //result += conditionResult; - for discriminatorless slicing, this would actually be "the" result, but
                        //we don't know we're using discriminatorless slicing here anymore.  For all other slicing, it is not
                        //necessary to know why we failed each individual case (except maybe for debugging purposes, but this would
                        //produce an enormous amount of information

                        // to add to slice
                        buckets.Add(Slices[sliceNumber], candidate);
                        break;
                    }
                }

                // So we found no slice that can take this candidate, let's pass it to the default slice
                if (!hasSucceeded)
                {
                    traces.Add(new Trace($"Input[{candidateNumber}] did not match any slice."));

                    defaultInUse = true;
                    buckets.AddDefault(candidate);
                }
            }

            var bucketAssertions = await buckets.Validate(vc).ConfigureAwait(false);

            return result += bucketAssertions + new Assertions(traces);
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

        private class Buckets : Dictionary<Slice, IList<ITypedElement>>
        {
            private readonly List<ITypedElement> _defaultBucket = new();
            private readonly IAssertion _defaultAssertion;

            public Buckets(IEnumerable<Slice> slices, IAssertion defaultAssertion)
            {
                // initialize the buckets according to the slice definitions
                foreach (var item in slices)
                {
                    this.Add(item, new List<ITypedElement>());
                }

                _defaultAssertion = defaultAssertion;
            }

            public void Add(Slice slice, ITypedElement item)
            {
                if (TryGetValue(slice, out var list)) list.Add(item);
            }

            public void AddDefault(ITypedElement item) => _defaultBucket.Add(item);

            public async Task<Assertions> Validate(ValidationContext vc)
                => await this.Select(slice => slice.Key.Assertion.Validate(slice.Value, vc))
                    .Append(_defaultAssertion.Validate(_defaultBucket, vc))
                    .AggregateAsync();
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

