/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
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
        /// for the the combined report is determined to be the weakest result of the combined reports.
        /// A non-successful report without evidence of its own (e.g. a bare <see cref="FAILURE"/>) is
        /// represented in the combined evidence by a fixed <see cref="ResultAssertion"/>, so its
        /// contribution to the outcome remains visible (and survives <see cref="TransformIssues"/>).</remarks>
        public static ResultReport Combine(IReadOnlyCollection<ResultReport> reports)
        {
            if (reports.Count == 0) return SUCCESS;
            if (reports.Count == 1) return reports.Single();

            var usefulEvidence = reports.Where(e => !isSuccessWithoutDetails(e)).ToList();

            if (usefulEvidence.Count == 1) return usefulEvidence.Single();

            var totalResult = usefulEvidence.Aggregate(ValidationResult.Success,
                (acc, elem) => acc.Combine(elem.Result));

            var flattenedEvidence = usefulEvidence.SelectMany(collectEvidence);

            return new ResultReport(totalResult, flattenedEvidence);

            static bool isSuccessWithoutDetails(ResultReport evidence) =>
                evidence == SUCCESS ||
                evidence.IsSuccessful && !evidence.Evidence.Any();

#pragma warning disable CS0618 // Type or member is obsolete
            static IEnumerable<IAssertion> collectEvidence(ResultReport report) =>
                report.Evidence.Count > 0 || report.IsSuccessful
                    ? report.Evidence
                    : [report.Result == ValidationResult.Failure ? ResultAssertion.FAILURE : ResultAssertion.UNDECIDED];
#pragma warning restore CS0618 // Type or member is obsolete
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

        /// <summary>
        /// Returns a report where each issue in the evidence has been passed through
        /// <paramref name="transformer"/>: issues for which the transformer returns <c>null</c> are
        /// removed (suppressed), issues for which it returns a different instance are replaced (e.g.
        /// to change their severity). Non-issue evidence is left untouched.
        /// </summary>
        /// <remarks>The <see cref="Result"/> of the returned report is recalculated from the
        /// transformed issues, so suppressing or downgrading all errors turns a failing report into a
        /// successful one. A failure or undecided outcome that was not caused by an issue in the
        /// evidence (e.g. a bare <see cref="FAILURE"/>) is preserved.</remarks>
        public ResultReport TransformIssues(IssueTransformer transformer)
        {
            var changed = false;
            var newEvidence = new List<IAssertion>(Evidence.Count);
            var originalIssueResult = ValidationResult.Success;
            var newIssueResult = ValidationResult.Success;

            // The part of the outcome that was not caused by issues cannot be affected by the
            // transformation and is kept as a floor for the new result. Fixed non-issue verdicts in
            // the evidence (see Combine) carry that floor explicitly.
            var nonIssueResult = ValidationResult.Success;

            foreach (var item in Evidence)
            {
                if (item is IssueAssertion issue)
                {
                    originalIssueResult = originalIssueResult.Combine(issue.Result);

                    if (transformer(issue) is { } transformed)
                    {
                        newIssueResult = newIssueResult.Combine(transformed.Result);
                        newEvidence.Add(transformed);
                        changed |= !ReferenceEquals(transformed, issue);
                    }
                    else
                        changed = true;
                }
                else
                {
                    if (item is IFixedResult fixedResult)
                        nonIssueResult = nonIssueResult.Combine(fixedResult.FixedResult);
                    newEvidence.Add(item);
                }
            }

            if (!changed) return this;

            // For reports that were constructed directly (rather than via Combine), a result that is
            // worse than what its issues explain must also have a non-issue cause - keep that too.
            if (originalIssueResult.Combine(Result) != originalIssueResult)
                nonIssueResult = nonIssueResult.Combine(Result);

            return new ResultReport(nonIssueResult.Combine(newIssueResult), newEvidence);
        }
    }

    /// <summary>
    /// A function that transforms an issue produced during validation, used with
    /// <see cref="ResultReport.TransformIssues(IssueTransformer)"/> and
    /// <see cref="ValidationSettings.TransformIssues"/>.
    /// </summary>
    /// <param name="issue">The issue produced by validation.</param>
    /// <returns>The issue to include in the result instead: the issue itself to keep it unchanged,
    /// a new <see cref="IssueAssertion"/> to replace it (e.g. with another severity), or <c>null</c>
    /// to suppress it.</returns>
    public delegate IssueAssertion? IssueTransformer(IssueAssertion issue);
}
