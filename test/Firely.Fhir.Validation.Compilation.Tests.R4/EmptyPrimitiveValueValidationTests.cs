/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    public class EmptyPrimitiveValueValidationTests
    {
        [Theory]
        [InlineData("value")]
        [InlineData(null)]
        [Trait("Category", "Validation")]
        public void FirelyValidatorTest_EmptyPrimitiveValue(string? labelValue)
        {
            // Arrange
            var extension = new Extension
            {
                Url = "http://hl7.org/fhir/StructureDefinition/translation",
                Extension = 
                [
                    new Extension("lang", new Code("nl-nl")),
                    new Extension("content", new FhirString("translation")),
                ] 
            };

            var sd = new StructureDefinition
            {
                BaseDefinition = ModelInfo.CanonicalUriForFhirCoreType(FHIRAllTypes.Patient)!,
                Type = ModelInfo.FhirTypeToFhirTypeName(FHIRAllTypes.Patient),
                Name = "TestPatient",
                Abstract = false,
                Status = PublicationStatus.Draft,
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Url = "http://example.org/fhir/StructureDefinition/TestPatient",
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new ElementDefinition("Patient.language")
                        {
                            ElementId = "Patient.language",
                            LabelElement = new FhirString(labelValue)
                            {
                                Extension = [extension]
                            }
                        }
                    ]
                }
            };

            var packageServer = "https://packages.simplifier.net";
            string[] packageNames =
            [
                "hl7.fhir.r4.core@4.0.1", 
            ];
            var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
            var resolver = new MultiResolver(new InMemoryProfileResolver(sd), packageResolver);

            var terminologyService = new LocalTerminologyService(resolver.AsAsync());
            var validator = new Validator(resolver.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(sd);

            // Assert
            outcome.Should().NotBeNull();
            outcome.Issue.Should().HaveCount(0);
        }
    }
}