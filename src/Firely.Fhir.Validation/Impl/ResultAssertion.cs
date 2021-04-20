/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The result of validation as determined by an assertion.
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>
        /// The instance was valid according to the rules of the assertion.
        /// </summary>
        Success,

        /// <summary>
        /// The instance failed the rules of the assertion.
        /// </summary>
        Failure,

        /// <summary>
        /// The validity could not be asserted.
        /// </summary>
        Undecided
    }

    /// <summary>
    /// Represents the outcome of a validating assertion, optionally listing the evidence 
    /// for the validation result.
    /// </summary>
    [DataContract]
    public class ResultAssertion : IAssertion, IMergeable
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
        public static ResultAssertion CreateFailure(params IAssertion[] evidence) =>
            new(ValidationResult.Failure, evidence);

        /// <summary>
        /// Creates a ResultAssertion with a succesful outcome and the evidence given.
        /// </summary>
        public static ResultAssertion CreateSuccess(params IAssertion[] evidence) =>
            new(ValidationResult.Success, evidence);

        public static ResultAssertion ForIssue(IssueAssertion issue) =>
            new(issue.Severity.ToValidationResult(), issue);

        public static ResultAssertion ForIssues(IEnumerable<IssueAssertion> issues) =>
                new(combineSeverities(issues), issues);

        /// <summary>
        /// Creates a new ResultAssertion where the result is derived from the members.
        /// </summary>
        public static ResultAssertion Combine(params ResultAssertion[] members) => Combine(members.AsEnumerable());

        /// <summary>
        /// Creates a new ResultAssertion where the result is derived from the members.
        /// </summary>
        /// <remarks>Members become the <see cref="ResultAssertion.Evidence"/> and the total result
        /// can never be stronger than those of the member results
        /// (<see cref="ValidationExtensions.Combine(ValidationResult, ValidationResult) />)"</remarks>
        public static ResultAssertion Combine(IEnumerable<ResultAssertion> members) =>
            new(combineResults(members), members);

        /// <summary>
        /// Creates a new ResultAssertion where the result is derived from issues and
        /// other results.
        /// </summary>
        /// <remarks>Members become the <see cref="ResultAssertion.Evidence"/> and the total result
        /// can never be stronger than those of the member results
        /// (<see cref="ValidationExtensions.Combine(ValidationResult, ValidationResult) />)"</remarks>
        public static ResultAssertion Combine(IEnumerable<IssueAssertion> issues, IEnumerable<ResultAssertion> members)
        {
            var totalResult = combineSeverities(issues).Combine(combineResults(members));
            var totalEvidence = ((IEnumerable<IAssertion>)issues).Union(members);
            return new(totalResult, totalEvidence);
        }

        private static ValidationResult combineResults(IEnumerable<ResultAssertion> results) =>
            results.Aggregate(ValidationResult.Success, (acc, elem) => acc.Combine(elem.Result));

        private static ValidationResult combineSeverities(IEnumerable<IssueAssertion> issues) =>
            issues.Aggregate(ValidationResult.Success,
                (acc, elem) => acc.Combine(elem.Severity.ToValidationResult()));

        /// <summary>
        /// Creates a ResultAssertion with the given outcome and evidence.
        /// </summary>
        public ResultAssertion(ValidationResult result, params IAssertion[] evidence) : this(result, evidence.AsEnumerable())
        {
        }

        /// <summary>
        /// Creates a ResultAssertion with a failed outcome and the evidence given.
        /// </summary>
        public ResultAssertion(ValidationResult result, IEnumerable<IAssertion> evidence)
        {
            Evidence = evidence?.ToArray() ?? throw new ArgumentNullException(nameof(evidence));
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
    }
}
