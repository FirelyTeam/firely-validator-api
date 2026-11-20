/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CardinalityValidatorTests
    {
        [TestMethod]
        public void IncorrectConstructorArguments1()
        {
            _ = Assert.ThrowsExactly<IncorrectElementDefinitionException>(() => new CardinalityValidator(-1, null));
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

        [TestMethod]
        public void IncorrectConstructorArguments6()
        {
            _ = Assert.ThrowsExactly<IncorrectElementDefinitionException>(() => CardinalityValidator.FromMinMax(7, "6"));
        }

        [TestMethod]
        public void IncorrectConstructorArguments7()
        {
            _ = Assert.ThrowsExactly<IncorrectElementDefinitionException>(() => CardinalityValidator.FromMinMax(0, "invalid"));
        }

        [TestMethod]
        public void IncorrectConstructorArguments8()
        {
            _ = Assert.ThrowsExactly<IncorrectElementDefinitionException>(() => CardinalityValidator.FromMinMax(0, "-1"));
        }

        [TestMethod]
        public void InRangeAsync()
        {
            var cardinality = new ChildrenValidator(true, ("given", CardinalityValidator.FromMinMax(2, "3")));

            var makeList = (List<string> primitives) => new HumanName { Given = primitives }.ToPocoNode();
            
            var result = cardinality.Validate(makeList(["a", "b", "c"]), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsTrue(result.IsSuccessful);

            result = cardinality.Validate(makeList(["a", "b", "c", "d"]), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);

            result = cardinality.Validate(makeList(["a"]), ValidationSettings.BuildMinimalContext(), new ValidationState());
            Assert.IsFalse(result.IsSuccessful);
        }

        [TestMethod]
        [DataRow("{\"resourceType\":\"Organization\",\"identifier\":{\"value\":\"x\"}}", false)]
        [DataRow("{\"resourceType\":\"Organization\",\"identifier\":[{\"value\":\"x\"}]}", true)]
        public void NarrowedRepeatingElementMustStillBeList(string json, bool expectSuccess)
        {
            var organization = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.SyntaxOnly)).DeserializeResource(json);
            var validator = new ChildrenValidator(true, ("identifier", CardinalityValidator.FromMinMax(0, "1")));

            var result = validator.Validate(organization.ToPocoNode(), ValidationSettings.BuildMinimalContext(), new ValidationState());

            Assert.AreEqual(expectSuccess, result.IsSuccessful);
        }

        [TestMethod]
        [DataRow("{\"resourceType\":\"Organization\",\"identifier\":{\"value\":\"x\"}}", false)]
        [DataRow("{\"resourceType\":\"Organization\",\"alias\":\"Firely\"}", false)]
        [DataRow("{\"resourceType\":\"Organization\",\"identifier\":[{\"value\":\"x\"}],\"alias\":[\"Firely\"]}", true)]
        [DataRow("{\"resourceType\":\"Organization\",\"name\":\"Firely\"}", true)]
        public void RepeatingElementMustBeList(string json, bool expectSuccess)
        {
            var organization = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.SyntaxOnly)).DeserializeResource(json);
            var validator = new ChildrenValidator(true, ("identifier", CardinalityValidator.FromMinMax(0, "*")), ("alias", CardinalityValidator.FromMinMax(0, "*")));

            var result = validator.Validate(organization.ToPocoNode(), ValidationSettings.BuildMinimalContext(), new ValidationState());

            Assert.AreEqual(expectSuccess, result.IsSuccessful);
            if (!expectSuccess)
                Assert.AreEqual(Issue.CONTENT_INCORRECT_OCCURRENCE.Code, result.Evidence.OfType<IssueAssertion>().Single().IssueNumber);
        }
    }
}
