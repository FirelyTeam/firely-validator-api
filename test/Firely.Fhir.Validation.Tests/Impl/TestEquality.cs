/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestEquality
    {
        [TestMethod]
        public void TestIssueAndTraceDuplication()
        {
            var dummy = new MaxLengthValidator(4);   // just an assertion that should not be de-duplicated
            var duplicateDummy = dummy;   // will be duplicated based on reference equality
            var trace = new TraceAssertion("location", "message");
            var duplicateTrace = new TraceAssertion("location", "message");
            var issue = new IssueAssertion(1, "message", Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Fatal, Hl7.Fhir.Model.OperationOutcome.IssueType.Expired);
            var duplicateIssue = new IssueAssertion(1, "message", Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Fatal, Hl7.Fhir.Model.OperationOutcome.IssueType.Expired);
            var anotherIssue = new IssueAssertion(2, "another message", Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Fatal, Hl7.Fhir.Model.OperationOutcome.IssueType.Expired);

            var evidence = new List<IAssertion>() { dummy, duplicateDummy, trace, issue, duplicateTrace, anotherIssue, duplicateIssue, duplicateDummy };
            var report = new ResultReport(ValidationResult.Success, evidence);

            report.Evidence.Count.Should().Be(evidence.Count);
            var report2 = report.RemoveDuplicateEvidence();
            report2.Evidence.Count.Should().Be(7 - 3);  // all duplicateXXXX removed

            report2.Evidence.Should().ContainSingle(e => ReferenceEquals(e, dummy));
            report2.Evidence.Should().ContainSingle(e => e is TraceAssertion);
            report2.Evidence.Where(e => e is IssueAssertion ia && ia.IssueNumber == 1).Should().ContainSingle();
            report2.Evidence.Where(e => e is IssueAssertion ia && ia.IssueNumber == 2).Should().ContainSingle();
        }
    }
}
