using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class AllAssertionTests
    {
        private class SuccessAssertion : IValidatable
        {
            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
            {
                return Task.FromResult(Assertions.SUCCESS + new Trace("Success Assertion"));
            }
        }

        private class FailureAssertion : IValidatable
        {
            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
            {
                return Task.FromResult(Assertions.FAILURE + new Trace("Failure Assertion"));
            }
        }


        [TestMethod]
        public async Task SingleOperand()
        {
            var allAssertion = new AllAssertion(new SuccessAssertion());
            var result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsTrue(result.Result.IsSuccessful);

            allAssertion = new AllAssertion(new FailureAssertion());
            result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsFalse(result.Result.IsSuccessful);

        }

        [TestMethod]
        public async Task Combinations()
        {
            var allAssertion = new AllAssertion(new SuccessAssertion(), new FailureAssertion());
            var result = await allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationContext.BuildMinimalContext()).ConfigureAwait(false);
            Assert.IsFalse(result.Result.IsSuccessful);

        }
    }
}
