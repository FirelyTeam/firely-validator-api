/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class AllValidatorTests
    {
        private class SuccessAssertion : IValidatable
        {
            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public ResultReport Validate(ITypedElement input, ValidationContext _, ValidationState __)
            {
                return new TraceAssertion(input.Location, "Success Assertion").AsResult();
            }
        }

        private class FailureAssertion : IValidatable
        {
            private readonly string _message;

            public FailureAssertion(string message = "Failure Assertion") { _message = message; }

            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
            {
                return
                    new ResultReport(ValidationResult.Failure,
                    new TraceAssertion(input.Location, _message));
            }
        }


        [TestMethod]
        public void SingleOperand()
        {
            var allAssertion = new AllValidator(new SuccessAssertion());
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext());
            Assert.IsTrue(result.IsSuccessful);

            allAssertion = new AllValidator(new FailureAssertion());
            result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext());
            Assert.IsFalse(result.IsSuccessful);
        }

        [TestMethod]
        public void Combinations()
        {
            var allAssertion = new AllValidator(new SuccessAssertion(), new FailureAssertion());
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext());
            Assert.IsFalse(result.IsSuccessful);

        }

        [TestMethod]
        public void MyTestMethod()
        {
            var allAssertion = new AllValidator(new SuccessAssertion(), new FailureAssertion("First failure"), new FailureAssertion("Second failure"));
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext());
            result.IsSuccessful.Should().Be(false);
            result.Evidence.OfType<TraceAssertion>()
                           .Should()
                           .NotContain(e => e.Message == "Second failure");
        }
    }
}
