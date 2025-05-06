/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class SchemaReferenceValidatorTests
    {
        [TestMethod]
        public void InvokesCorrectSchema()
        {
            var schemaUri = "http://someotherschema";
            var schema = new ElementSchema(schemaUri, new ChildrenValidator(true, ("value", new FixedValidator(new FhirString("hi").ToTypedElement()))));
            var resolver = new TestResolver() { schema };
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            var instance = new Extension()
            {
                Url = "http://extensionschema.nl",
                Value = new FhirString("hi")
            };

            var refv = new SchemaReferenceValidator(schemaUri);

            var result = refv.Validate(instance.ToTypedElement(), vc);
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsTrue(resolver.ResolvedSchemas.Contains(schemaUri));
            Assert.AreEqual(1, resolver.ResolvedSchemas.Count);
        }


        [TestMethod]
        public void ExtensionInvokesCorrectSchema()
        {
            var schemaUri = "http://extensionschema.nl";
            var extSchema = new ExtensionSchema(
                new StructureDefinitionInformation("http://hl7.org/fhir/StructureDefinition/Extension", null, "Extension", null, false));
            var referredSchema = new ExtensionSchema(
                new StructureDefinitionInformation(schemaUri, null, "Extension", null, false),
                new ChildrenValidator(true, ("value", new FixedValidator(new FhirString("hi").ToTypedElement()))));

            var resolver = new TestResolver() { referredSchema };
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            var instance = new Extension
            {
                Url = "http://extensionschema.nl",
                Value = new FhirString("hi")
            };

            var result = extSchema.Validate(instance.ToTypedElement(), vc);
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsTrue(resolver.ResolvedSchemas.Contains(schemaUri));
            Assert.AreEqual(1, resolver.ResolvedSchemas.Count);
        }

        private readonly PocoNode _dummyData =
            new FhirBoolean(true).ToPocoNode();

        [TestMethod]
        public void InvokesMissingSchema()
        {
            var schema = new SchemaReferenceValidator("http://example.org/non-existant");
            var resolver = new TestResolver(); // empty resolver with no profiles installed
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            var result = schema.Validate(_dummyData, vc);
            result.Evidence.Should().ContainSingle().Which.Should().BeOfType<IssueAssertion>().Which
                .IssueNumber.Should().Be(Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);
        }

        [DataTestMethod]
        [DataRow("#Subschema1", true)]
        [DataRow("#Subschema2", true)]
        [DataRow("#Subschema3", true)]
        [DataRow("#Subschema4", false)]
        public void InvokedSubschema(string subschema, bool success)
        {
            var schema = new ElementSchema("http://example.org/rootSchema",
                new DefinitionsAssertion(
                    new ElementSchema("#Subschema1", ResultAssertion.SUCCESS),
                    new ElementSchema("#Subschema2", ResultAssertion.SUCCESS)
                    ),
                new DefinitionsAssertion(
                    new ElementSchema("#Subschema3", ResultAssertion.SUCCESS)
                    )
                );

            var resolver = new TestResolver(new[] { schema });
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            var refSchema = new SchemaReferenceValidator(schema.Id! + subschema);
            var result = refSchema.Validate(_dummyData, vc);
            Assert.AreEqual(success, result.IsSuccessful);
        }
    }
}
