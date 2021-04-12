/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public enum ValidationResult
    {
        Success,
        Failure,
        Undecided
    }

    /// <summary>
    /// Asserts that the validation was successful, was a failure, or had an undecided outcome.
    /// </summary>
    [DataContract]
    public class ResultAssertion : IAssertion, IMergeable, IValidatable
    {
        public static readonly ResultAssertion SUCCESS = new(ValidationResult.Success);
        public static readonly ResultAssertion FAILURE = new(ValidationResult.Failure);
        public static readonly ResultAssertion UNDECIDED = new(ValidationResult.Undecided);

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public readonly ValidationResult Result;

        [DataMember(Order = 1)]
        public readonly IAssertion[] Evidence;
#else
        [DataMember]
        public ValidationResult Result { get; }

        [DataMember]
        public IAssertion[] Evidence { get; }
#endif

        public static ResultAssertion CreateFailure(params IAssertion[] evidence) => new(ValidationResult.Failure, evidence);

        public ResultAssertion(ValidationResult result, params IAssertion[] evidence) : this(result, evidence.AsEnumerable())
        {
        }

        public ResultAssertion(ValidationResult result, IEnumerable<IAssertion> evidence)
        {
            Evidence = evidence?.ToArray() ?? throw new ArgumentNullException(nameof(evidence));
            Result = result;
        }

        public bool IsSuccessful => Result == ValidationResult.Success;

        public IMergeable Merge(IMergeable other)
        {
            if (other is ResultAssertion ra)
            {
                // If we currently are succesful, the new result fully depends on the other
                // Otherwise, we are failing or undecided, which we need to
                // propagate
                // if other is not succesful as well, then combine the evidence as a result
                return new ResultAssertion(IsSuccessful ? ra.Result : Result, Evidence.Concat(ra.Evidence));
            }
            else
                throw Error.InvalidOperation($"Internal logic failed: tried to merge a ResultAssertion with an {other.GetType().Name}");
        }

        public JToken ToJson()
        {
            var raise = new JObject(
                new JProperty("result", Result.ToString()));

            if (Evidence.Any())
            {
                var evidence = new JArray(Evidence.Select(e => e.ToJson().MakeNestedProp()));
                raise.Add(new JProperty("evidence", evidence));
            }

            return new JProperty("raise", raise);
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var result = new Assertions(this);
            foreach (var item in Evidence)
            {
                result += await item.Validate(input, vc, state).ConfigureAwait(false);
            }
            return result;
        }


    }
}
