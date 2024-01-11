/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ChildrenValidatorTests
    {
        [TestMethod]
        public void ValidateCorrectNumberOfChildren()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1", "child2" });

            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Evidence.Count);
            Assert.AreEqual("child1", getElementAt(result, 0));
            Assert.AreEqual("child2", getElementAt(result, 1));
        }

        [TestMethod]
        public void ValidateMoreChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1", "child2", "child3" });

            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());

            Assert.IsFalse(result.IsSuccessful);
            result.FailedWith(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code);
            result.FailedWith("child1");
            result.FailedWith("child2");
        }

        [TestMethod]
        public void ValidateLessChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1" });

            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Evidence.Count);
            Assert.AreEqual("child1", getElementAt(result, 0));

        }

        [TestMethod]
        public void ValidateOtherChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "childA", "childB" });


            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Evidence.OfType<TraceAssertion>().Count());
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getFailureEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public void ValidateEmptyChildren()
        {
            var assertion = new ChildrenValidator(false);
            var input = createNode(new[] { "child1", "child2" });

            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getFailureEvidence(result)?.IssueNumber);
        }

        private static IssueAssertion? getFailureEvidence(ResultReport assertions)
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

        private static string? getElementAt(ResultReport assertions, int index)
            => assertions.Evidence[index] is IssueAssertion ta ? ta.Message : null;

        private static IDictionary<string, IAssertion> createTuples(string[] childNames) =>
            childNames.ToDictionary(s => s,
                s => new IssueAssertion(-10, s, OperationOutcome.IssueSeverity.Information) as IAssertion);
    }
}