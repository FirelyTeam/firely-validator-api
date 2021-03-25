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
            var sub = new ElementSchema("#sub", new Trace("In a subschema"));

            var main = new ElementSchema("http://root.nl/schema1",
                new Definitions(sub),
                new ElementSchema("#nested", new Trace("nested")),
                new ReferenceAssertion(sub.Id),
                new SliceAssertion(false, true,
                    @default: new Trace("this is the default"),
                    new SliceAssertion.Slice("und", ResultAssertion.UNDECIDED, new Trace("I really don't know")),
                    new SliceAssertion.Slice("fail", ResultAssertion.FAILURE, new Trace("This always fails"))
                    ),

                new Children(false,
                    ("child1", new ElementSchema(new Trace("in child 1"))),
                    ("child2", new Trace("in child 2")))
                );

            var result = main.ToJson().ToString();
        }

        [TestMethod]
        public async Task ValidateSchema()
        {
            var stringSchema = new ElementSchema("#string",
                new Assertions(
                    new MaxLength(50),
                    new FhirTypeLabel("string")
                )
            );

            var familySchema = new ElementSchema("#myHumanName.family",
                new Assertions(
                    new ReferenceAssertion(stringSchema.Id),
                    new CardinalityAssertion(0, "1", "myHumanName.family"),
                    new MaxLength(40),
                    new Fixed("Brown")
                )
            );

            var givenSchema = new ElementSchema("#myHumanName.given",
                new Assertions(
                    new ReferenceAssertion(stringSchema.Id),
                    new CardinalityAssertion(0, "*", "myHumanName.given"),
                    new MaxLength(40)
                )
            );

            var myHumanNameSchema = new ElementSchema("http://example.com/myHumanNameSchema",
                new Definitions(stringSchema),
                new Children(false,
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

            var vc = new ValidationContext() { ElementSchemaResolver = schemaResolver };

            var validationResults = await myHumanNameSchema.Validate(humanName, vc).ConfigureAwait(false);

            Assert.IsNotNull(validationResults);
            Assert.IsFalse(validationResults.Result.IsSuccessful);

            var issues = validationResults.GetIssueAssertions();
            issues.Should()
                .Contain(i => i.IssueNumber == Issue.CONTENT_INCORRECT_OCCURRENCE.Code && i.Location == "myHumanName.family", "maximum is 1")
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
            private readonly Dictionary<System.Uri, IElementSchema> _schemas;

            public InMemoryElementSchemaResolver(IEnumerable<IElementSchema> schemas)
            {
                _schemas = schemas.ToDictionary(s => s.Id);
            }

            public Task<IElementSchema?> GetSchema(System.Uri schemaUri) => Task.FromResult(_schemas.TryGetValue(schemaUri, out var schema) ? schema : null);
        }

        [TestMethod]
        public async Task ValidateBloodPressureSchema()
        {
            var bpComponentSchema = new ElementSchema("#bpComponentSchema",
                new Assertions(
                    new CardinalityAssertion(1, "1"),
                    new Children(false,
                        ("code", new CardinalityAssertion(1, "*")),
                        ("value[x]", new AllAssertion(new CardinalityAssertion(1, "*"), new FhirTypeLabel("Quantity")))
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

            var systolicSlice = new SliceAssertion.Slice("systolic",
                    new PathSelectorAssertion("code", new Fixed(buildCodeableConcept("http://loinc.org", "8480-6"))),
                bpComponentSchema
            );

            var dystolicSlice = new SliceAssertion.Slice("dystolic",
                    new PathSelectorAssertion("code", new Fixed(buildCodeableConcept("http://loinc.org", "8462-4"))),
                bpComponentSchema
            );


            var componentSchema = new ElementSchema("#ComponentSlicing",
                new Assertions(
                    new CardinalityAssertion(2, "*"),
                    new SliceAssertion(false, false, ResultAssertion.SUCCESS, new[] { systolicSlice, dystolicSlice })
                    )
            );

            var bloodPressureSchema = new ElementSchema("http://example.com/bloodPressureSchema",
                new Children(false,
                    ("status", new CardinalityAssertion(1, "*")),
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


            var json = bloodPressureSchema.ToJson().ToString();

            var vc = new ValidationContext();

            var validationResults = await bloodPressureSchema.Validate(bloodPressure, vc).ConfigureAwait(false);

            Assert.IsTrue(validationResults.Result.IsSuccessful);
            validationResults.GetIssueAssertions().Should().BeEmpty();
        }
    }
}
