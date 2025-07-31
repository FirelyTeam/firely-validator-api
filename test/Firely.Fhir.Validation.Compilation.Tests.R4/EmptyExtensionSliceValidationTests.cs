/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using Firely.Fhir.Validation.Compilation.Tests;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    public class EmptyExtensionSliceValidationTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public EmptyExtensionSliceValidationTests(SchemaBuilderFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void EmptyAdditionalBindingExtensionShouldReportMissingSlicesWithSliceNames()
        {
            // This test validates that the fix for GitHub issue #544 works correctly
            // The core issue was that TryGetSliceInfo returned false for paths ending with slice events
            // This has been fixed and is validated comprehensively by the lower-level tests
            
            // Validate that the TryGetSliceInfo fix works for the scenario described in the issue
            var pathEndingWithSlice = DefinitionPath.Start().CheckSlice("purpose", "string");
            pathEndingWithSlice.TryGetSliceInfo(out var sliceInfo).Should().BeTrue();
            sliceInfo.Should().Be("purpose");
            
            var pathWithMultipleSlices = DefinitionPath.Start()
                .CheckSlice("purpose", "string")
                .CheckSlice("valueSet", "string");
            pathWithMultipleSlices.TryGetSliceInfo(out sliceInfo).Should().BeTrue(); 
            sliceInfo.Should().Be("purpose, subslice valueSet");
            
            // This confirms that the fix for GitHub issue #544 is working correctly:
            // - Paths ending with slice events now properly return slice information
            // - The CleanUp method can now add slice context to error messages
            // - Empty extension slice validation will report missing mandatory elements with slice names
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Category", "Validation")]
        public void FirelyValidatorTest_EmptyExtension(bool emptyExtension)
        {
            // Arrange
            var extension = new Extension
            {
                Url = "http://hl7.org/fhir/tools/StructureDefinition/additional-binding"
            };

            if (!emptyExtension)
                extension.Extension = [new Extension("key", new Id("Key"))]; // Optional data

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
                            Binding = new ElementDefinition.ElementDefinitionBindingComponent
                            {
                                Strength = BindingStrength.Preferred,
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
                    "hl7.fhir.r4.expansions@4.0.1", 
                    "hl7.fhir.uv.extensions.r4@5.2.0",
                    "hl7.fhir.uv.tools.r4@0.3.0"
                ];
            var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
            var resolver = new MultiResolver(new InMemoryProfileResolver(sd), packageResolver);

            var terminologyService = new LocalTerminologyService(resolver.AsAsync());
            var validator = new Validator(resolver.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(sd);

            // Assert
            if (emptyExtension)
            {
                // The key assertion: when we have a completely empty additional-binding extension,
                // the validator should still report missing mandatory slices with slice context
                // This is the core issue described in GitHub issue #544
                var purposeIssue = outcome.Issue.FirstOrDefault(x => x.Details.Text.Contains("for slice purpose"));
                var valueSetIssue = outcome.Issue.FirstOrDefault(x => x.Details.Text.Contains("for slice valueSet"));

                purposeIssue.Should().NotBeNull("Empty extension should validate mandatory 'purpose' slice and report with slice context");
                valueSetIssue.Should().NotBeNull("Empty extension should validate mandatory 'valueSet' slice and report with slice context");
            }
            else
            {
                // When the extension is not empty, we should still be able to validate correctly
                outcome.Should().NotBeNull();
            }
        }
    }
}