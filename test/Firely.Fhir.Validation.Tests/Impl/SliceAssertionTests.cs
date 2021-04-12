﻿/* 
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
    public class SliceAssertionTests
    {
        [TestMethod]
        public async Task TestUnordered()
        {
            var result = await Test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = await Test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "slice1"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice1Evidence);

            result = await Test(
                    buildSliceAssertion(ordered: false, openAtEnd: false),
                    buildTestcase("slice1", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                 buildSliceAssertion(ordered: false, openAtEnd: true),
                 buildTestcase("default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, DefaultEvidence);

            result = await Test(
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
            var result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2", "default"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence);

            result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("default", "slice1", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "default", "slice2"));
            result.IsSuccessful.Should().BeTrue();
            testEvidence(result.Evidence, Slice2Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1", "default"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            var ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence[1..], Slice1Evidence, Slice2Evidence, DefaultEvidence);

            result = await Test(
                    buildSliceAssertion(ordered: true, openAtEnd: false),
                    buildTestcase("slice2", "slice1"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence[0].Should().BeOfType<IssueAssertion>();
            ia = (IssueAssertion)result.Evidence[0];
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_SLICING_OUT_OF_ORDER.Code);
            testEvidence(result.Evidence[1..], Slice1Evidence, Slice2Evidence);
        }

        private void testEvidence(IAssertion[] actual, params Trace[] expected) =>
            actual.Should().BeEquivalentTo(expected,
                option => option.IncludingAllRuntimeProperties().WithStrictOrdering());

        async Task<ResultAssertion> Test(SliceAssertion assertion, IEnumerable<ITypedElement> instances)
        {
            var vc = ValidationContext.BuildMinimalContext();
            return (await assertion.Validate(instances, vc)).Result;
        }

        IEnumerable<ITypedElement> buildTestcase(params string[] instances) =>
            instances.Select(i => ElementNode.ForPrimitive(i));

        static ResultAssertion successAssertion(Trace message) => new ResultAssertion(ValidationResult.Success,
                    message);

        internal readonly Trace Slice1Evidence = new Trace("You've hit slice 1.");
        internal readonly Trace Slice2Evidence = new Trace("You've hit slice 2.");
        internal readonly Trace DefaultEvidence = new Trace("You've hit the default.");

        SliceAssertion buildSliceAssertion(bool ordered, bool openAtEnd) =>
            new SliceAssertion(ordered, openAtEnd, successAssertion(DefaultEvidence),
                new SliceAssertion.Slice("slice1", new Fixed(ElementNode.ForPrimitive("slice1")), successAssertion(Slice1Evidence)),
                new SliceAssertion.Slice("slice2", new Fixed(ElementNode.ForPrimitive("slice2")), successAssertion(Slice2Evidence)));

    }
}
