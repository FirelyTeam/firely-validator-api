/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
        /// Creates a new <see cref="ResultReport"/> where the result is derived from multiple others
        /// reports.
        /// </summary>
        /// <remarks>All evidence is combined in the returned result and the <see cref="ResultReport.Result"/>
        /// for the the combined report is determined to be the weakest result of the combined reports.</remarks>
        public static ResultReport Combine(IReadOnlyCollection<ResultReport> reports)
        {
            if (reports.Count == 0) return SUCCESS;
            if (reports.Count == 1) return reports.Single();

            var usefulEvidence = reports.Where(e => !isSuccessWithoutDetails(e)).ToList();

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
