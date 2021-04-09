using Firely.Fhir.Validation.Tests.Impl;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
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
