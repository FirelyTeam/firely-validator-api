/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
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
    public class SliceValidatorTests
    {
        [TestMethod]
        public void TestUnordered()
        {
            var result = test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice1"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice1Evidence);

            result = test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, DefaultEvidence);

            result = test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("slice1", "default", "slice2"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            var ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE.Code);

            testEvidence(result.Evidence.Skip(1), Slice1Evidence, Slice2Evidence, DefaultEvidence);
        }

        [TestMethod]
        public void TestOrdered()
        {
            var result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("default", "slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice2Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1", "default"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            var ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence.Skip(1), Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence.Skip(1), Slice1Evidence, Slice2Evidence);
        }

        private static void testEvidence(IEnumerable<IAssertion> actual, params TraceAssertion[] expected) =>
            actual.Should().BeEquivalentTo(expected,
                option => option.ComparingByMembers<TraceAssertion>().Excluding(ta => ta.Location).WithStrictOrdering());

        private static ResultReport test(SliceValidator assertion, IEnumerable<ITypedElement> instances)
        {
            var vc = ValidationSettings.BuildMinimalContext();
            vc.TraceEnabled = true;
            return assertion.Validate(instances, vc);
        }

        private static IEnumerable<ITypedElement> buildTestcase(params string[] instances) =>
            instances.Select(i => ElementNode.ForPrimitive(i));

        internal readonly TraceAssertion Slice1Evidence = new("@primitivevalue@", "You've hit slice 1.");
        internal readonly TraceAssertion Slice2Evidence = new("@primitivevalue@", "You've hit slice 2.");
        internal readonly TraceAssertion DefaultEvidence = new("@primitivevalue@", "You've hit the default.");

        private SliceValidator buildSliceAssertion(bool ordered, bool openAtEnd) =>
            new(ordered, openAtEnd, DefaultEvidence,
                new SliceValidator.SliceCase("slice1", new FixedValidator(new FhirString("slice1")), Slice1Evidence),
                new SliceValidator.SliceCase("slice2", new FixedValidator(new FhirString("slice2")), Slice2Evidence));

    }
}
