/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

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
            var cardinality = CardinalityValidator.FromMinMax(2, "3");

            PocoListNode makeList(IEnumerable<PrimitiveType> items)
            {
                return new PocoListNode(items.ToArray(), null, "value");
            }
            
            var result = cardinality.Validate(makeList(Enumerable.Repeat<PrimitiveType>(new Integer(1), 3)), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsTrue(result.IsSuccessful);

            result = cardinality.Validate(makeList(Enumerable.Repeat<PrimitiveType>(new Integer(1), 4)), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);

            result = cardinality.Validate(makeList(Enumerable.Repeat<PrimitiveType>(new Integer(1), 1)), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);
        }
    }
}
