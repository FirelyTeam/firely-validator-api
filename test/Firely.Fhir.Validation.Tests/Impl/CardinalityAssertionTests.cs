using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CardinalityAssertionTests
    {
        [ExpectedException(typeof(IncorrectElementDefinitionException), "Lower cardinality cannot be lower than 0.")]
        [TestMethod]
        public void IncorrectConstructorArguments1()
        {
            _ = new CardinalityAssertion(-1, null);
        }

        [TestMethod]
        public void IncorrectConstructorArguments2()
        {
            _ = new CardinalityAssertion(null, null);

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments3()
        {
            _ = CardinalityAssertion.FromMinMax(0, "*");

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments4()
        {
            _ = new CardinalityAssertion(0, null);

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments5()
        {
            _ = CardinalityAssertion.FromMinMax(0, "1");
            _ = new CardinalityAssertion(0, 1);

            // no failures expected
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality must be higher than the lower cardinality.")]
        [TestMethod]
        public void IncorrectConstructorArguments6()
        {
            _ = CardinalityAssertion.FromMinMax(7, "6");
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality shall be a positive number or '*'.")]
        [TestMethod]
        public void IncorrectConstructorArguments7()
        {
            _ = CardinalityAssertion.FromMinMax(0, "invalid");
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality cannot be lower than 0.")]
        [TestMethod]
        public void IncorrectConstructorArguments8()
        {
            _ = CardinalityAssertion.FromMinMax(0, "-1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task InRangeAsync()
        {
            var cardinality = new CardinalityAssertion(2, 3);

            var result = await cardinality.Validate(ElementNode.CreateList("1", 1, 9L), ValidationContext.BuildMinimalContext());
            Assert.IsTrue(result.Result.IsSuccessful);

            result = await cardinality.Validate(ElementNode.CreateList("1", 1, 9L, 2), ValidationContext.BuildMinimalContext());
            Assert.IsFalse(result.Result.IsSuccessful);

            result = await cardinality.Validate(ElementNode.CreateList("1"), ValidationContext.BuildMinimalContext());
            Assert.IsFalse(result.Result.IsSuccessful);
        }
    }
}
