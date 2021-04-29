/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

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

            public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
            {
                return Task.FromResult(
                    ResultAssertion.CreateSuccess(
                    new TraceAssertion(input.Location, "Success Assertion")));
            }
        }

        private class FailureAssertion : IValidatable
        {
            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
            {
                return Task.FromResult(
                    ResultAssertion.CreateFailure(
                    new TraceAssertion(input.Location, "Failure Assertion")));
            }
        }


        [TestMethod]
        public async Task SingleOperand()
        {
            var allAssertion = new AllValidator(new SuccessAssertion());
            var result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful);

            allAssertion = new AllValidator(new FailureAssertion());
            result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsFalse(result.IsSuccessful);
        }

        [TestMethod]
        public async Task Combinations()
        {
            var allAssertion = new AllValidator(new SuccessAssertion(), new FailureAssertion());
            var result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsFalse(result.IsSuccessful);

        }
    }
}
