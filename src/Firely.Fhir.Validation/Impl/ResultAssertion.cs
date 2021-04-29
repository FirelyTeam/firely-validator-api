/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the outcome of a validating assertion, optionally listing the evidence 
    /// for the validation result.
    /// </summary>
    [DataContract]
    public class ResultAssertion : IResultAssertion, IValidatable, IMergeable
    {
        /// <summary>
        /// Represents a success assertion without evidence.
        /// </summary>
        public static readonly ResultAssertion SUCCESS = new(ValidationResult.Success);

        /// <summary>
        /// Represents a failure assertion without evidence.
        /// </summary>
        public static readonly ResultAssertion FAILURE = new(ValidationResult.Failure);

        /// <summary>
        /// Represents an undecided outcome without evidence.
        /// </summary>
        public static readonly ResultAssertion UNDECIDED = new(ValidationResult.Undecided);

#if MSGPACK_KEY
        /// <summary>
        /// The result of the validation.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly ValidationResult Result;

        /// <summary>
        /// Evidence for the result.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly IAssertion[] Evidence;
#else
        /// <summary>
        /// The result of the validation.
        /// </summary>
        [DataMember]
        public ValidationResult Result { get; }

        /// <summary>
        /// Evidence for the result.
        /// </summary>
        [DataMember]
        public IAssertion[] Evidence { get; }
#endif

        /// <summary>
        /// Creates a ResultAssertion with a failed outcome and the evidence given.
        /// </summary>
        /// <remarks>Note that the outcome will be <see cref="ValidationResult.Failure" />,
        /// regardless of the evidence provided.</remarks>
        public static ResultAssertion CreateFailure(params IAssertion[] evidence) =>
            new(ValidationResult.Failure, evidence);

        /// <summary>
        /// Creates a ResultAssertion with a succesful outcome and the evidence given.
        /// </summary>
        /// <remarks>Note that the outcome will be <see cref="ValidationResult.Success" />,
        /// regardless of the evidence provided.</remarks>
        public static ResultAssertion CreateSuccess(params IAssertion[] evidence) =>
            new(ValidationResult.Success, evidence);

        /// <inheritdoc cref="FromEvidence(IEnumerable{IAssertion})"/>
        public static ResultAssertion FromEvidence(params IAssertion[] evidence) =>
            FromEvidence(evidence.AsEnumerable());

        /// <summary>
        /// Creates a new ResultAssertion where the result is derived from the evidence.
        /// </summary>
        /// <remarks>The evidence is included in the returned result and the total result
        /// for the the ResultAssertion is calculated to be the weakest of the evidence.
        public static ResultAssertion FromEvidence(IEnumerable<IAssertion> evidence)
        {
            var usefulEvidence = evidence.Where(e => !isSuccessWithoutDetails(e)).ToList();

            if (usefulEvidence.Count == 1 && usefulEvidence.Single() is ResultAssertion ra) return ra;

            var totalResult = usefulEvidence.Aggregate(ValidationResult.Success,
                (acc, elem) => acc.Combine(deriveResult(elem)));

            var flattenedEvidence = usefulEvidence.SelectMany(ue =>
                    ue switch
                    {
                        ResultAssertion ra => ra.Evidence,
                        var nonRa => new[] { nonRa }
                    });

            return new ResultAssertion(totalResult, flattenedEvidence);

            static ValidationResult deriveResult(IAssertion assertion) =>
                assertion switch
                {
                    IResultAssertion ra => ra.Result,
                    _ => ValidationResult.Success
                };

            static bool isSuccessWithoutDetails(IAssertion evidence) =>
                evidence == SUCCESS ||
                evidence is ResultAssertion ra &&
                    ra.IsSuccessful &&
                    !ra.Evidence.Any();
        }

        /// <summary>
        /// Creates a ResultAssertion with the given outcome and evidence.
        /// </summary>
        public ResultAssertion(ValidationResult result, params IAssertion[] evidence) : this(result, evidence.AsEnumerable())
        {
        }

        /// <summary>
        /// Creates a ResultAssertion with the given outcome and evidence.
        /// </summary>
        public ResultAssertion(ValidationResult result, IEnumerable<IAssertion> evidence)
        {
            Evidence = evidence.ToArray();
            Result = result;
        }

        /// <summary>
        /// Whether the result indicates a success.
        /// </summary>
        public bool IsSuccessful => Result == ValidationResult.Success;

        /// <inheritdoc />
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            // Validation does not mean anything more than using this instance as a prototype and
            // turning the result assertion into another result by cloning it as a prototype and
            // setting the runtime location of it and its evidence.  Note that this is only done
            // when Validate() is called, which is when
            // this assertion is part of a generated schema (e.g. the default case in a slice),
            // not when instances of ResultAssertion are used as returned results of a Validator.
            var revisitedEvidence = (await Evidence
                .Select(e => e.Validate(input, vc, state))
                .AggregateAssertions()).Evidence;

            // Note, the result is cloned and it takes whatever the result of the prototype
            // is, regardless of the result of validating the evidence.
            return new ResultAssertion(Result, revisitedEvidence);
        }
    }
}
