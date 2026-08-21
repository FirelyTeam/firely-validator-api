/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class SeverityOverrideTests
    {
        private static readonly ValidationSettings SETTINGS = ValidationSettings.BuildMinimalContext();

        // A failing input for every concrete InvariantValidator implementation, so a severity
        // override is proven to work on the generic FhirPath validator AND the hand-coded
        // fast-path validators alike.
        private static IEnumerable<(InvariantValidator validator, PocoNode failingInput)> failingCases()
        {
            yield return (new FhirPathValidator("test-1", "false"), new HumanName { Given = ["a"] }.ToPocoNode());
            yield return (new FhirEle1Validator(), new HumanName().ToPocoNode());
            yield return (new FhirExt1Validator(), new Extension().ToPocoNode());
            yield return (new FhirTxt1Validator(), new HumanName().ToPocoNode());
            yield return (new FhirTxt2Validator(), PocoNode.ForPrimitive<FhirString>(" "));
        }

        [TestMethod]
        public void OverrideDowngradesFailingInvariantsToWarnings()
        {
            foreach (var (validator, input) in failingCases())
            {
                var name = validator.GetType().Name;

                // without the override, the invariant fails with the error-constraint issue
                var original = ((IValidatable)validator).Validate(input, SETTINGS, new ValidationState());
                original.IsSuccessful.Should().BeFalse($"{name} should fail on its failing input");
                original.GetIssues().Single().IssueNumber.Should()
                    .Be(Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT.Code, $"{name} should report an error constraint");

                // 'with' produces a copy of the same concrete type, carrying the override
                var downgraded = validator with { SeverityOverride = OperationOutcome.IssueSeverity.Warning };
                downgraded.GetType().Should().Be(validator.GetType());

                // the downgraded invariant emits the warning-constraint issue, flipping the outcome
                var result = ((IValidatable)downgraded).Validate(input, SETTINGS, new ValidationState());
                result.IsSuccessful.Should().BeTrue($"{name} with a warning override should not fail validation");
                result.GetIssues().Single().IssueNumber.Should()
                    .Be(Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT.Code, $"{name} should report a warning constraint");
            }
        }

        [TestMethod]
        public void EqualityIgnoresCompilationCache()
        {
            var a = new FhirPathValidator("test-1", "true");
            var b = new FhirPathValidator("test-1", "true");

            a.Equals(b).Should().BeTrue();
            var hash = a.GetHashCode();

            // validating compiles and caches the expression inside the validator - being a record,
            // its equality and hash code must nevertheless remain stable
            _ = ((IValidatable)a).Validate(new HumanName { Given = ["a"] }.ToPocoNode(), SETTINGS, new ValidationState());

            a.Equals(b).Should().BeTrue();
            a.GetHashCode().Should().Be(hash);

            // while a semantic difference (like a severity override) does make them unequal
            (a with { SeverityOverride = OperationOutcome.IssueSeverity.Warning }).Equals(b).Should().BeFalse();
        }

        [TestMethod]
        public void OverrideTakesPrecedenceOverBestPracticeMapping()
        {
            // for a best-practice invariant, the effective severity normally comes from the
            // ConstraintBestPractices setting - an explicit override must win over that too
            var bestPractice = new FhirPathValidator("bp-1", "false", "a best practice", OperationOutcome.IssueSeverity.Warning, bestPractice: true);
            var settings = ValidationSettings.BuildMinimalContext();
            settings.ConstraintBestPractices = ValidateBestPracticesSeverity.Error;

            var input = new HumanName { Given = ["a"] }.ToPocoNode();

            ((IValidatable)bestPractice).Validate(input, settings, new ValidationState())
                .IsSuccessful.Should().BeFalse("the best-practice setting escalates the invariant to an error");

            var downgraded = bestPractice with { SeverityOverride = OperationOutcome.IssueSeverity.Warning };
            var result = ((IValidatable)downgraded).Validate(input, settings, new ValidationState());
            result.IsSuccessful.Should().BeTrue("the override wins over the best-practice mapping");
            result.GetIssues().Single().IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT.Code);
        }
    }
}
