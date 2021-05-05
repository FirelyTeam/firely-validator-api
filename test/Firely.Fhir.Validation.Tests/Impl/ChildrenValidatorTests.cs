/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ChildrenValidatorTests
    {
        [TestMethod]
        public async Task ValidateCorrectNumberOfChildren()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1", "child2" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Evidence.Count);
            Assert.AreEqual("child1", getElementAt(result, 0));
            Assert.AreEqual("child2", getElementAt(result, 1));
        }

        [TestMethod]
        public async Task ValidateMoreChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1", "child2", "child3" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());

            Assert.IsFalse(result.IsSuccessful);
            result.FailedWith(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code);
            result.FailedWith("child1");
            result.FailedWith("child2");
        }

        [TestMethod]
        public async Task ValidateLessChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Evidence.Count);
            Assert.AreEqual("child1", getElementAt(result, 0));

        }

        [TestMethod]
        public async Task ValidateOtherChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "childA", "childB" });


            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Evidence.OfType<TraceAssertion>().Count());
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getFailureEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public async Task ValidateEmptyChildren()
        {
            var assertion = new ChildrenValidator(false);
            var input = createNode(new[] { "child1", "child2" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getFailureEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public async Task ValidateNoChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(Array.Empty<string>());

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_MUST_HAVE_VALUE_OR_CHILDREN.Code, getFailureEvidence(result)?.IssueNumber);
        }

        private static IssueAssertion getFailureEvidence(ResultAssertion assertions)
            => assertions.Evidence
            .OfType<IssueAssertion>()
            .FirstOrDefault(ia => ia.Result != ValidationResult.Success);

        private static ITypedElement createNode(string[] childNames)
        {
            var result = ElementNodeAdapter.Root("root");
            foreach (var childName in childNames)
            {
                result.Add(childName);
            }
            return result;
        }

        private static string? getElementAt(ResultAssertion assertions, int index)
            => assertions.Evidence[index] is IssueAssertion ta ? ta.Message : null;

        private static IDictionary<string, IAssertion> createTuples(string[] childNames) =>
            childNames.ToDictionary(s => s,
                s => new IssueAssertion(-10, s, OperationOutcome.IssueSeverity.Information) as IAssertion);
    }
}