/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the outcome of a validating assertion, optionally listing the evidence 
    /// for the validation result.
    /// </summary>
    [DataContract]
    public class ResultReport
    {
        /// <summary>
        /// Represents a success assertion without evidence.
        /// </summary>
        public static readonly ResultReport SUCCESS = new(ValidationResult.Success);

        /// <summary>
        /// Represents a failure assertion without evidence.
        /// </summary>
        public static readonly ResultReport FAILURE = new(ValidationResult.Failure);

        /// <summary>
        /// Represents an undecided outcome without evidence.
        /// </summary>
        public static readonly ResultReport UNDECIDED = new(ValidationResult.Undecided);

        /// <summary>
        /// The result of the validation.
        /// </summary>
        [DataMember]
        public ValidationResult Result { get; }

        /// <summary>
        /// Evidence for the result.
        /// </summary>
        [DataMember]
        public IReadOnlyList<IAssertion> Evidence { get; }

        /// <summary>
        /// Creates a new ResultAssertion where the result is derived from the evidence.
        /// </summary>
        /// <remarks>The evidence is included in the returned result and the total result
        /// for the the ResultAssertion is calculated to be the weakest of the evidence.</remarks>
        public static ResultReport FromEvidence(IReadOnlyCollection<ResultReport> evidence)
        {
            if (evidence.Count == 0) return SUCCESS;
            if (evidence.Count == 1) return evidence.Single();

            var usefulEvidence = evidence.Where(e => !isSuccessWithoutDetails(e)).ToList();

            if (usefulEvidence.Count == 1) return usefulEvidence.Single();

            var totalResult = usefulEvidence.Aggregate(ValidationResult.Success,
                (acc, elem) => acc.Combine(elem.Result));

            var flattenedEvidence = usefulEvidence.SelectMany(ue => ue.Evidence);

            return new ResultReport(totalResult, flattenedEvidence);

            static bool isSuccessWithoutDetails(ResultReport evidence) =>
                evidence == SUCCESS ||
                evidence.IsSuccessful && !evidence.Evidence.Any();
        }

        /// <summary>
        /// Creates a ResultAssertion with the given outcome and evidence.
        /// </summary>
        public ResultReport(ValidationResult result, params IAssertion[] evidence) : this(result, evidence.AsEnumerable())
        {
        }

        /// <summary>
        /// Creates a ResultAssertion with the given outcome and evidence.
        /// </summary>
        public ResultReport(ValidationResult result, IEnumerable<IAssertion> evidence)
        {
            Evidence = evidence.ToArray();
            Result = result;
        }

        /// <summary>
        /// Whether the result indicates a success.
        /// </summary>
        public bool IsSuccessful => Result == ValidationResult.Success;

        /// <summary>
        /// Returns any warnings that are part of the evidence for this result.
        /// </summary>
        public IReadOnlyCollection<IssueAssertion> Warnings => GetIssues(OperationOutcome.IssueSeverity.Warning);

        /// <summary>
        /// Returns any errors that are part of the evidence for this result.
        /// </summary>
        public IReadOnlyCollection<IssueAssertion> Errors => GetIssues(OperationOutcome.IssueSeverity.Error);

        /// <summary>
        /// Returns issues (optionally filtering on the given severity) that are part of the evidence for this result.
        /// </summary>
        public IReadOnlyCollection<IssueAssertion> GetIssues(OperationOutcome.IssueSeverity? severity = null) =>
            severity switch
            {
                null => Evidence.OfType<IssueAssertion>().ToList(),
                _ => Evidence.OfType<IssueAssertion>().Where(ia => ia.Severity == severity).ToList()
            };
    }
}
