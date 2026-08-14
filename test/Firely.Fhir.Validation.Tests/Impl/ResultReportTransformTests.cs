/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ResultReportTransformTests
    {
        private static IssueAssertion error() =>
            new(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN, "an error");

        private static IssueAssertion warning() =>
            new(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, "a warning", IssueSeverity.Warning);

        [TestMethod]
        public void SuppressingAllErrorsTurnsFailureIntoSuccess()
        {
            var report = new ResultReport(ValidationResult.Failure, error(), warning());

            var transformed = report.TransformIssues(_ => null);

            Assert.IsTrue(transformed.IsSuccessful);
            Assert.AreEqual(0, transformed.Evidence.Count);
        }

        [TestMethod]
        public void DowngradingAnErrorToWarningFlipsTheResult()
        {
            var report = new ResultReport(ValidationResult.Failure, error());

            var transformed = report.TransformIssues(issue =>
                new IssueAssertion(issue.IssueNumber, issue.Message, IssueSeverity.Warning, issue.Type));

            Assert.IsTrue(transformed.IsSuccessful);
            var issue = transformed.GetIssues().Single();
            Assert.AreEqual(IssueSeverity.Warning, issue.Severity);
            Assert.AreEqual("an error", issue.Message);
        }

        [TestMethod]
        public void SuppressingOnlySomeIssuesKeepsTheRemainingResult()
        {
            var report = new ResultReport(ValidationResult.Failure, error(), warning());

            var transformed = report.TransformIssues(issue => issue.Severity == IssueSeverity.Warning ? null : issue);

            Assert.IsFalse(transformed.IsSuccessful);
            Assert.AreEqual(IssueSeverity.Error, transformed.GetIssues().Single().Severity);
        }

        [TestMethod]
        public void FailureNotCausedByIssuesIsPreserved()
        {
            // this failure is not explained by its (warning) issue, so suppression cannot lift it
            var report = new ResultReport(ValidationResult.Failure, warning());

            var transformed = report.TransformIssues(_ => null);

            Assert.IsFalse(transformed.IsSuccessful);
            Assert.AreEqual(0, transformed.Evidence.Count);
        }

        [TestMethod]
        public void FailureFloorSurvivesCombiningAndSuppression()
        {
            // A bare FAILURE has no evidence of its own, so after Combine() flattens the evidence its
            // contribution would be indistinguishable from the error issue's. Combine represents it as
            // a fixed ResultAssertion, so suppressing all issues cannot lift the failure.
            var combined = ResultReport.Combine([new ResultReport(ValidationResult.Failure, error()), ResultReport.FAILURE]);

            Assert.IsFalse(combined.IsSuccessful);
            Assert.IsTrue(combined.Evidence.Contains(ResultAssertion.FAILURE), "the bare failure should be visible in the evidence");

            var transformed = combined.TransformIssues(_ => null);

            Assert.IsFalse(transformed.IsSuccessful);
            Assert.AreSame(ResultAssertion.FAILURE, transformed.Evidence.Single());
        }

        [TestMethod]
        public void UnchangedIssuesReturnTheSameReportInstance()
        {
            var report = new ResultReport(ValidationResult.Failure, error(), warning());

            var transformed = report.TransformIssues(issue => issue);

            Assert.AreSame(report, transformed);
        }

        [TestMethod]
        public void NonIssueEvidenceIsLeftUntouched()
        {
            var trace = new TraceAssertion("Patient.name[0]", "a trace");
            var report = new ResultReport(ValidationResult.Failure, trace, error());

            var transformed = report.TransformIssues(_ => null);

            Assert.IsTrue(transformed.IsSuccessful);
            Assert.AreSame(trace, transformed.Evidence.Single());
        }
    }
}
