/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
            Assert.AreEqual(2, result.Evidence.Length);
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
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getEvidence(result)?.IssueNumber);
            Assert.AreEqual(2, result.Evidence.OfType<Visited>().Count(), "matched 2 children");
            Assert.AreEqual("child1", getElementAt(result, 0));
            Assert.AreEqual("child2", getElementAt(result, 1));
        }

        [TestMethod]
        public async Task ValidateLessChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "child1" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Evidence.Length);
            Assert.AreEqual("child1", getElementAt(result, 0));

        }

        [TestMethod]
        public async Task ValidateOtherChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(new[] { "childA", "childB" });


            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Evidence.OfType<Visited>().Count());
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public async Task ValidateEmptyChildren()
        {
            var assertion = new ChildrenValidator(false);
            var input = createNode(new[] { "child1", "child2" });

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Evidence.OfType<Visited>().Any());
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN.Code, getEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public async Task ValidateNoChildrenThenDefined()
        {
            var assertion = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var input = createNode(Array.Empty<string>());

            var result = await assertion.Validate(input, ValidationContext.BuildMinimalContext());
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Evidence.OfType<Visited>().Any());
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(Issue.CONTENT_ELEMENT_MUST_HAVE_VALUE_OR_CHILDREN.Code, getEvidence(result)?.IssueNumber);
        }

        [TestMethod]
        public void MergeTest()
        {
            var assertion1 = new ChildrenValidator(createTuples(new[] { "child1", "child2" }), false);
            var assertion2 = new ChildrenValidator(createTuples(new[] { "child3", "child1" }), false);

            var result = assertion1.Merge(assertion2) as ChildrenValidator;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ChildrenValidator));

            Assert.AreEqual(3, result!.ChildList.Count);
            Assert.AreEqual(3, result!.ChildList.Keys.Intersect(new[] { "child1", "child2", "child3" }).Count());
        }

        private static IssueAssertion getEvidence(ResultAssertion assertions)
            => assertions.Evidence.OfType<IssueAssertion>().FirstOrDefault();

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
            => assertions.Evidence[index] is Visited v ? v.ChildName : null;

        private static IDictionary<string, IAssertion> createTuples(string[] childNames)
        {
            var result = new Dictionary<string, IAssertion>();
            foreach (var childName in childNames)
            {
                result.Add(childName, new VisitorAssertion(childName));
            }
            return result;
        }
    }

    internal class VisitorAssertion : IValidatable
    {
        private readonly IAssertion _visited;

        public VisitorAssertion(string childName)
        {
            _visited = new Visited { ChildName = childName };
        }

        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            return Task.FromResult(ResultAssertion.FromEvidence(_visited));
        }

        public JToken ToJson()
        {
            throw new System.NotImplementedException();
        }
    }

    internal class Visited : IAssertion
    {
        public JToken ToJson()
        {
            throw new System.NotImplementedException();
        }

        public string? ChildName { get; set; }
    }
}