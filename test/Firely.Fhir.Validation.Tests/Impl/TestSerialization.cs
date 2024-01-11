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
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestSerialization
    {
        [TestMethod]
        public void SerializeSchema()
        {
            var sub = new ElementSchema("#sub", new TraceAssertion("sub", "In a subschema"));

            var main = new ElementSchema("http://root.nl/schema1",
                new DefinitionsAssertion(sub),
                new ElementSchema("#nested", new TraceAssertion("nested", "nested")),
                new ReferencedInstanceValidator(new ElementSchema("#forReference", new TraceAssertion("forReference", "validation rules")),
                        new[] { AggregationMode.Contained }, ReferenceVersionRules.Either),
                new SchemaReferenceValidator("http://root.nl/schema1#" + sub.Id),
                new SchemaReferenceValidator("http://root.nl/schema1#" + sub.Id + "2"),
                new SliceValidator(false, true,
                    @default: new TraceAssertion("default", "this is the default"),
                    new SliceValidator.SliceCase("und", ResultAssertion.UNDECIDED, new TraceAssertion("und", "I really don't know")),
                    new SliceValidator.SliceCase("fail", ResultAssertion.FAILURE, new TraceAssertion("fail", "This always fails"))
                    ),

                new ChildrenValidator(false,
                    ("child1", new ElementSchema("id", new TraceAssertion("child1", "in child 1"))),
                    ("child2", new TraceAssertion("child2", "in child 2")))
                );

            var result = main.ToJson().ToString();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ValidateSchema()
        {
            var stringSchema = new ElementSchema("http://test.org/string",
                    new MaxLengthValidator(50),
                    new FhirTypeLabelValidator("string")
            );

            var familySchema = new ElementSchema("#family",
                    new SchemaReferenceValidator(stringSchema.Id),
                    new CardinalityValidator(0, 1),
                    new MaxLengthValidator(40),
                    new FixedValidator(new FhirString("Brown"))
            );

            var givenSchema = new ElementSchema("#given",
                    new SchemaReferenceValidator(stringSchema.Id),
                    CardinalityValidator.FromMinMax(0, "*"),
                    new MaxLengthValidator(40)
            );

            var myHumanNameSchema = new DatatypeSchema(
                new(
                    "http://example.com/myHumanNameSchema", null,
                    "HumanName",
                    StructureDefinitionInformation.TypeDerivationRule.Specialization, false)
            ,
                new DefinitionsAssertion(stringSchema),
                new ChildrenValidator(false,
                    ("family", familySchema),
                    ("given", givenSchema)
                )
            );

            var humanName = ElementNodeAdapter.Root("HumanName");
            humanName.Add("family", "Brown", "string");
            humanName.Add("family", "Brown2", "string");
            humanName.Add("given", "Joe", "string");
            humanName.Add("given", "Patrick", "string");
            humanName.Add("given", new string('x', 41), "string");
            humanName.Add("given", "1", "integer");


            var result = myHumanNameSchema.ToJson().ToString();

            var schemaResolver = new InMemoryElementSchemaResolver(new[] { stringSchema });

            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: schemaResolver);
            var validationResults = myHumanNameSchema.Validate(humanName, vc);

            Assert.IsNotNull(validationResults);
            Assert.IsFalse(validationResults.IsSuccessful);

            var issues = validationResults.Evidence.OfType<IssueAssertion>().ToList();
            issues.Should()
                .Contain(i => i.IssueNumber == Issue.CONTENT_INCORRECT_OCCURRENCE.Code && i.Location == "HumanName.family", "cardinality of 0..1")
                .And
                .Contain(i => i.IssueNumber == Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE.Code && i.Location == "HumanName.family[1]", "fixed to Brown")
                .And
                .Contain(i => i.IssueNumber == Issue.CONTENT_ELEMENT_VALUE_TOO_LONG.Code && i.Location == "HumanName.given[2]", "HumanName.given[2] is too long")
                .And
                .Contain(i => i.IssueNumber == Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE.Code && i.Location == "HumanName.given[3]", "HumanName.given must be of type string")
                .And.HaveCount(4);
        }

        private class InMemoryElementSchemaResolver : IElementSchemaResolver
        {
            private readonly Dictionary<Canonical, ElementSchema> _schemas;

            public InMemoryElementSchemaResolver(IEnumerable<ElementSchema> schemas)
            {
                _schemas = schemas.ToDictionary(s => s.Id);
            }

            public ElementSchema? GetSchema(Canonical schemaUri) =>
                _schemas.TryGetValue(schemaUri, out var schema) ? schema : null;
        }

        [TestMethod]
        public void ValidateBloodPressureSchema()
        {
            var bpComponentSchema = new ElementSchema("#bpComponentSchema",
                    new CardinalityValidator(1, 1),
                    new ChildrenValidator(false,
                        ("code", new CardinalityValidator(min: 1)),
                        ("value[x]", new AllValidator(new CardinalityValidator(min: 1), new FhirTypeLabelValidator("Quantity")))
                    )
            ); ;

            static ITypedElement buildCodeableConcept(string system, string code)
            {
                var coding = ElementNodeAdapter.Root("Coding");
                coding.Add("system", system, "string");
                coding.Add("code", code, "string");

                var result = ElementNodeAdapter.Root("CodeableConcept");
                result.Add(coding, "coding");
                return result;
            }

            static CodeableConcept buildCodeableConceptPoco(string system, string code)
            {
                var result = new CodeableConcept();
                result.Coding.Add(new Coding(system, code));
                return result;
            }

            var systolicSlice = new SliceValidator.SliceCase("systolic",
                    new PathSelectorValidator("code", new FixedValidator(buildCodeableConceptPoco("http://loinc.org", "8480-6"))),
                bpComponentSchema
            );

            var dystolicSlice = new SliceValidator.SliceCase("dystolic",
                    new PathSelectorValidator("code", new FixedValidator(buildCodeableConceptPoco("http://loinc.org", "8462-4"))),
                bpComponentSchema
            );


            var componentSchema = new ElementSchema("#ComponentSlicing",
                    new CardinalityValidator(min: 2),
                    new SliceValidator(false, false, ResultAssertion.SUCCESS, new[] { systolicSlice, dystolicSlice })
            );

            var bloodPressureSchema = new ElementSchema("http://example.com/bloodPressureSchema",
                new ChildrenValidator(false,
                    ("status", new CardinalityValidator(min: 1)),
                    ("component", componentSchema)
                )
            );

            static ITypedElement buildBpComponent(string system, string code, string value)
            {
                var result = ElementNodeAdapter.Root("Component");
                result.Add(buildCodeableConcept(system, code), "code");
                result.Add("value", value, "Quantity");
                return result;
            }

            var bloodPressure = ElementNodeAdapter.Root("Observation");
            bloodPressure.Add("status", "final", "string");
            bloodPressure.Add(buildBpComponent("http://loinc.org", "8480-6", "120"), "component");
            bloodPressure.Add(buildBpComponent("http://loinc.org", "8462-4", "80"), "component");

            var vc = ValidationSettings.BuildMinimalContext();
            var validationResults = bloodPressureSchema.Validate(bloodPressure, vc);

            Assert.IsTrue(validationResults.IsSuccessful);
            validationResults.Evidence.OfType<IssueAssertion>().Should().BeEmpty();
        }
    }
}
