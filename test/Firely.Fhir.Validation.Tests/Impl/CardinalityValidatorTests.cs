/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Sdk.Benchmarks;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CardinalityValidatorTests
    {
        [ExpectedException(typeof(IncorrectElementDefinitionException), "Lower cardinality cannot be lower than 0.")]
        [TestMethod]
        public void IncorrectConstructorArguments1()
        {
            _ = new CardinalityValidator(-1, null);
        }

        [TestMethod]
        public void IncorrectConstructorArguments2()
        {
            _ = new CardinalityValidator(null, null);

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments3()
        {
            _ = CardinalityValidator.FromMinMax(0, "*");

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments4()
        {
            _ = new CardinalityValidator(0, null);

            // no failures expected
        }

        [TestMethod]
        public void IncorrectConstructorArguments5()
        {
            _ = CardinalityValidator.FromMinMax(0, "1");
            _ = new CardinalityValidator(0, 1);

            // no failures expected
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality must be higher than the lower cardinality.")]
        [TestMethod]
        public void IncorrectConstructorArguments6()
        {
            _ = CardinalityValidator.FromMinMax(7, "6");
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality shall be a positive number or '*'.")]
        [TestMethod]
        public void IncorrectConstructorArguments7()
        {
            _ = CardinalityValidator.FromMinMax(0, "invalid");
        }

        [ExpectedException(typeof(IncorrectElementDefinitionException), "Upper cardinality cannot be lower than 0.")]
        [TestMethod]
        public void IncorrectConstructorArguments8()
        {
            _ = CardinalityValidator.FromMinMax(0, "-1");
        }

        [TestMethod]
        public void InRangeAsync()
        {
            var cardinality = new CardinalityValidator(2, 3);

            var result = cardinality.Validate(ElementNode.CreateList("1", 1, 9L), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsTrue(result.IsSuccessful);

            result = cardinality.Validate(ElementNode.CreateList("1", 1, 9L, 2), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);

            result = cardinality.Validate(ElementNode.CreateList("1"), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);
        }

        [TestMethod]
        [Ignore]
        public void TestFromEvidence()
        {
            var b = new ValidatorBenchmarks();
            b.GlobalSetup();
            b.WipValidator();
        }
    }
}
