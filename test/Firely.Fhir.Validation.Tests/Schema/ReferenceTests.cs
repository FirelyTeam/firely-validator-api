using Firely.Fhir.Validation.Tests.Impl;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    // The problem here is that /Reference doesn't by itself know that it should fetch the
    // reference using a ResourceReferenceAssertion. This is actually constructed when converting
    // a full TypeRef (all that logic is there). But you'd really want the logic to be in the
    // /Reference schema (I think it used to be baked in into the SchemaAssertion, or what it was called
    // back then - but that would bake FHIR specific knowledge into such an assertion...)
    [TestClass, Ignore("Schema and Reference have changed so much that this simple setup won't work anymore.")]
    public class ReferenceTests
    {
        private static readonly ElementSchema _schema = new("http://hl7.org/fhir/StructureDefinition/Patient",
                new Children(true,
                    ("id", new ElementSchema("#Patient.id")),
                    ("contained", new SchemaAssertion("http://hl7.org/fhir/StructureDefinition/Resource")),
                    ("other", new SchemaAssertion("http://hl7.org/fhir/StructureDefinition/Reference"))
                ),
                new ResultAssertion(ValidationResult.Success)
            );


        [TestMethod]
        public async Task CircularInContainedResources()
        {
            // circular in contained patients
            var pat = new
            {
                resourceType = "Patient",
                id = "pat1",
                contained = new[]
                {
                    new
                    {
                        resourceType = "Patient",
                        id = "pat2a",
                        other = new { reference = "#pat2b" }
                    },
                    new
                    {
                        resourceType = "Patient",
                        id = "pat2b",
                        other = new { reference = "#pat2a" }
                    }
                }
            };

            var result = await Test(_schema, pat.ToTypedElement("Patient"));
            result.IsSuccessful.Should().BeFalse();
        }

        [TestMethod]
        public async Task MultipleReferencesToResource()
        {
            var pat = new
            {
                resourceType = "Patient",
                id = "pat1",
                contained = new[]
                {
                    new
                    {
                        resourceType = "Patient",
                        id = "pat2a",
                    }
                },
                other = new[]
                {
                    new { reference = "#pat2a" },
                    new { reference = "#pat2a" }
                }
            };

            var result = await Test(_schema, pat.ToTypedElement("Patient"));
            result.IsSuccessful.Should().BeTrue();
        }

        static async Task<ResultAssertion> Test(ElementSchema schema, ITypedElement instance)
        {
            var resolver = new TestResolver() { schema };
            var vc = ValidationContext.BuildMinimalContext(schemaResolver: resolver);
            return (await schema.Validate(instance, vc)).Result;
        }
    }
}
