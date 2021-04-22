/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                new ReferencedInstanceValidator("reference", new ElementSchema("#forReference", new TraceAssertion("forReference", "validation rules")),
                        new[] { AggregationMode.Contained }, ReferenceVersionRules.Either),
                new SchemaReferenceValidator(sub.Id),
                new SchemaReferenceValidator(sub.Id + "2"),
                new SliceValidator(false, true,
                    @default: new TraceAssertion("default", "this is the default"),
                    new SliceValidator.SliceCase("und", ResultAssertion.UNDECIDED, new TraceAssertion("und", "I really don't know")),
                    new SliceValidator.SliceCase("fail", ResultAssertion.FAILURE, new TraceAssertion("fail", "This always fails"))
                    ),

                new ChildrenValidator(false,
                    ("child1", new ElementSchema(new TraceAssertion("child1", "in child 1"))),
                    ("child2", new TraceAssertion("child2", "in child 2")))
                );

            var result = main.ToJson().ToString();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ValidateSchema()
        {
            var stringSchema = new ElementSchema("#string",
                new Assertions(
                    new MaxLengthValidator(50),
                    new FhirTypeLabelValidator("string")
                )
            );

            var familySchema = new ElementSchema("#myHumanName.family",
                new Assertions(
                    new SchemaReferenceValidator(stringSchema.Id),
                    new CardinalityValidator(0, 1),
                    new MaxLengthValidator(40),
                    new FixedValidator("Brown")
                )
            );

            var givenSchema = new ElementSchema("#myHumanName.given",
                new Assertions(
                    new SchemaReferenceValidator(stringSchema.Id),
                    CardinalityValidator.FromMinMax(0, "*"),
                    new MaxLengthValidator(40)
                )
            );

            var myHumanNameSchema = new ElementSchema("http://example.com/myHumanNameSchema",
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

            var vc = ValidationContext.BuildMinimalContext(schemaResolver: schemaResolver);
            var validationResults = await myHumanNameSchema.Validate(humanName, vc).ConfigureAwait(false);

            Assert.IsNotNull(validationResults);
            Assert.IsFalse(validationResults.Result.IsSuccessful);

            var issues = validationResults.GetIssueAssertions().ToList();
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
            private readonly Dictionary<System.Uri, ElementSchema> _schemas;

            public InMemoryElementSchemaResolver(IEnumerable<ElementSchema> schemas)
            {
                _schemas = schemas.ToDictionary(s => s.Id);
            }

            public Task<ElementSchema?> GetSchema(System.Uri schemaUri) => Task.FromResult(_schemas.TryGetValue(schemaUri, out var schema) ? schema : null);
        }

        [TestMethod]
        public async Task ValidateBloodPressureSchema()
        {
            var bpComponentSchema = new ElementSchema("#bpComponentSchema",
                new Assertions(
                    new CardinalityValidator(1, 1),
                    new ChildrenValidator(false,
                        ("code", new CardinalityValidator(min: 1)),
                        ("value[x]", new AllValidator(new CardinalityValidator(min: 1), new FhirTypeLabelValidator("Quantity")))
                    )
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

            var systolicSlice = new SliceValidator.SliceCase("systolic",
                    new PathSelectorValidator("code", new FixedValidator(buildCodeableConcept("http://loinc.org", "8480-6"))),
                bpComponentSchema
            );

            var dystolicSlice = new SliceValidator.SliceCase("dystolic",
                    new PathSelectorValidator("code", new FixedValidator(buildCodeableConcept("http://loinc.org", "8462-4"))),
                bpComponentSchema
            );


            var componentSchema = new ElementSchema("#ComponentSlicing",
                new Assertions(
                    new CardinalityValidator(min: 2),
                    new SliceValidator(false, false, ResultAssertion.SUCCESS, new[] { systolicSlice, dystolicSlice })
                    )
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

            var vc = ValidationContext.BuildMinimalContext();
            var validationResults = await bloodPressureSchema.Validate(bloodPressure, vc).ConfigureAwait(false);

            Assert.IsTrue(validationResults.Result.IsSuccessful);
            validationResults.GetIssueAssertions().Should().BeEmpty();
        }
    }
}
