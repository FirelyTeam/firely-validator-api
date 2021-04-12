﻿using Firely.Fhir.Validation.Tests.Impl;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ReferenceTests
    {
        private static readonly ElementSchema SCHEMA = new("#patientschema",
                new Children(true,
                    ("id", new ElementSchema("#Patient.id")),
                    ("contained", new SchemaAssertion("#patientschema")),
                    ("other", new ResourceReferenceAssertion("reference", new SchemaAssertion("#patientschema")))
                ),
                new ResultAssertion(ValidationResult.Success)
            );


        [TestMethod]
        public async Task CircularInReferencedResources()
        {
            // circular in contained patients
            var pat1 = new
            {
                resourceType = "Patient",
                id = "http://example.com/pat1",
                other = new { reference = "http://example.com/pat2" }
            }.ToTypedElement();

            var pat2 = new
            {
                resourceType = "Patient",
                id = "http://example.com/pat2",
                other = new { reference = "http://example.com/pat1" }
            }.ToTypedElement();

            var resolver = new TestResolver() { SCHEMA };
            var vc = ValidationContext.BuildMinimalContext(schemaResolver: resolver);

            Task<ITypedElement?> resolveExample(string example) =>
                Task.FromResult(example switch
                {
                    "http://example.com/pat1" => pat1,
                    "http://example.com/pat2" => pat2,
                    _ => null
                });

            vc.ExternalReferenceResolver = resolveExample;
            var result = (await SCHEMA.Validate(pat1, vc)).Result;
            result.IsSuccessful.Should().BeFalse();
            result.Evidence.Should().ContainSingle().Which.Should().BeOfType<IssueAssertion>()
                .Which.IssueNumber.Should().Be(1018);
        }

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

            var result = await test(SCHEMA, pat.ToTypedElement("Patient"));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence.Should().HaveCount(2).And.AllBeOfType<IssueAssertion>().And
                .OnlyContain(ass => ((IssueAssertion)ass).IssueNumber == 1018);
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

            var result = await test(SCHEMA, pat.ToTypedElement("Patient"));
            result.IsSuccessful.Should().BeTrue();
        }

        private static async Task<ResultAssertion> test(ElementSchema schema, ITypedElement instance)
        {
            var resolver = new TestResolver() { schema };
            var vc = ValidationContext.BuildMinimalContext(schemaResolver: resolver);
            return (await schema.Validate(instance, vc)).Result;
        }
    }
}
