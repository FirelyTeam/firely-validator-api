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
    public class ReferenceTests
    {
        private static readonly ResourceSchema SCHEMA = new(new StructureDefinitionInformation("http://test.org/patientschema", null!, "Patient", null, false),
                new ChildrenValidator(true,
                    ("id", new ElementSchema("#Patient.id")),
                    ("contained", new SchemaReferenceValidator("http://test.org/patientschema")),
                    ("link", new ChildrenValidator(true, ("other" ,new ReferencedInstanceValidator(new SchemaReferenceValidator("http://test.org/patientschema")))))
                ),
                ResultAssertion.SUCCESS
            );


        [TestMethod]
        public void CircularInReferencedResources()
        {
            // circular in contained patients
            var pat1 = new Patient
            {
                Id = "http://example.com/pat1",
                Link = [new (){ Other = new ("http://example.com/pat2") }]
            }.ToTypedElement();

            var pat2 = new Patient
            {
                Id = "http://example.com/pat2",
                Link = [new (){ Other = new ("http://example.com/pat1") }]
            }.ToTypedElement();

            var resolver = new TestResolver() { SCHEMA };
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            PocoNode? resolveExample(string example, string location) =>
            example switch
            {
                "http://example.com/pat1" => pat1,
                "http://example.com/pat2" => pat2,
                _ => null
            };

            vc.ResolveExternalReference = resolveExample;
            var result = SCHEMA.Validate(pat1, vc);
            result.IsSuccessful.Should().BeTrue();  // this is a warning
            result.Evidence.Should().ContainSingle().Which.Should().BeOfType<IssueAssertion>()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_REFERENCE_CYCLE_DETECTED.Code);
        }

        [TestMethod]
        public void CircularInContainedResources()
        {
            
            // write the dict above as a poco
            var pat = new Patient
            {
                Id = "pat1",
                Contained = [
                    new Patient
                    {
                        Id = "pat2a",
                        Link = [new() {Other = new("#pat2b")}]
                    },
                    new Patient
                    {
                        Id = "pat2b",
                        Link = [new() {Other = new("#pat2a")}]
                    }
                ]
            };

            var result = test(SCHEMA, pat.ToTypedElement());
            result.IsSuccessful.Should().BeTrue();
            result.Evidence.Should().Contain(ass => (ass as IssueAssertion)!.IssueNumber == Issue.CONTENT_REFERENCE_CYCLE_DETECTED.Code);
        }

        [TestMethod]
        public void MultipleReferencesToResource()
        {
            // rewrite as poco. keep in mind other has to be wrapped in a link
            var pat = new Patient
            {
                Id = "pat1",
                Contained =
                [
                    new Patient { Id = "pat2a", }
                ],
                Link =
                [
                    new() { Other = new("#pat2a") },
                    new() { Other = new("#pat2a") }
                ]
            };

            var result = test(SCHEMA, pat.ToTypedElement());
            result.IsSuccessful.Should().BeTrue();
        }

        private static ResultReport test(ElementSchema schema, PocoNode instance)
        {
            var resolver = new TestResolver() { schema };
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);
            return schema.Validate(instance, vc);
        }
    }
}
