/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class SliceValidatorTests
    {
        [TestMethod]
        public async Task TestUnordered()
        {
            var result = await test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = await test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice1"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice1Evidence);

            result = await test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, DefaultEvidence);

            result = await test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("slice1", "default", "slice2"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            var ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE.Code);

            testEvidence(result.Evidence[1..], Slice1Evidence, Slice2Evidence, DefaultEvidence);
        }

        [TestMethod]
        public async Task TestOrdered()
        {
            var result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("default", "slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice2Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1", "default"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            var ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence[1..], Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence[1..], Slice1Evidence, Slice2Evidence);
        }

        private static void testEvidence(IAssertion[] actual, params TraceAssertion[] expected) =>
            actual.Should().BeEquivalentTo(expected,
                option => option.IncludingAllRuntimeProperties().WithStrictOrdering());

        private static async Task<ResultAssertion> test(SliceValidator assertion, IEnumerable<ITypedElement> instances)
        {
            var vc = ValidationContext.BuildMinimalContext();
            return (await assertion.Validate(instances, vc)).Result;
        }

        private static IEnumerable<ITypedElement> buildTestcase(params string[] instances) =>
            instances.Select(i => ElementNode.ForPrimitive(i));

        private static ResultAssertion successAssertion(TraceAssertion message) => new(ValidationResult.Success,
                    message);

        internal readonly TraceAssertion Slice1Evidence = new("You've hit slice 1.");
        internal readonly TraceAssertion Slice2Evidence = new("You've hit slice 2.");
        internal readonly TraceAssertion DefaultEvidence = new("You've hit the default.");

        private SliceValidator buildSliceAssertion(bool ordered, bool openAtEnd) =>
            new(ordered, openAtEnd, successAssertion(DefaultEvidence),
                new SliceValidator.SliceAssertion("slice1", new FixedValidator(ElementNode.ForPrimitive("slice1")), successAssertion(Slice1Evidence)),
                new SliceValidator.SliceAssertion("slice2", new FixedValidator(ElementNode.ForPrimitive("slice2")), successAssertion(Slice2Evidence)));

    }
}
