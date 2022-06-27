/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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

            public ResultAssertion Validate(ITypedElement input, ValidationContext _, ValidationState __)
            {
                return
                    ResultAssertion.FromEvidence(
                    new TraceAssertion(input.Location, "Success Assertion"));
            }
        }

        private class FailureAssertion : IValidatable
        {
            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState state)
            {
                return
                    new ResultAssertion(ValidationResult.Failure,
                    new TraceAssertion(input.Location, "Failure Assertion"));
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
    }
}
