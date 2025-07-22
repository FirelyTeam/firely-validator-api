/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using M = Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class EmptyExtensionSliceValidationTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void EmptyAdditionalBindingExtensionShouldReportMissingSlicesWithSliceNames(bool emptyExtension)
        {
            // This test implements Rob's suggested test case from GitHub issue #544
            // It ensures that when slice validation fails for empty additional-binding extensions,
            // the error messages include slice names ("for slice purpose", "for slice valueSet")
            
            // Arrange - Create the additional-binding extension as in Rob's test
            var extension = new M.Extension
            {
                Url = "http://hl7.org/fhir/tools/StructureDefinition/additional-binding"
            };

            if (!emptyExtension)
                extension.Extension = [new M.Extension("key", new M.Id("Key"))]; // Optional data

            var sd = new M.StructureDefinition
            {
                BaseDefinition = M.ModelInfo.CanonicalUriForFhirCoreType(M.FHIRAllTypes.Patient)!,
                Type = M.ModelInfo.FhirTypeToFhirTypeName(M.FHIRAllTypes.Patient),
                Name = "TestPatient",
                Abstract = false,
                Status = M.PublicationStatus.Draft,
                Kind = M.StructureDefinition.StructureDefinitionKind.Resource,
                Url = "http://example.org/fhir/StructureDefinition/TestPatient",
                Differential = new M.StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new M.ElementDefinition("Patient.language")
                        {
                            ElementId = "Patient.language",
                            Binding = new M.ElementDefinition.ElementDefinitionBindingComponent
                            {
                                Strength = M.BindingStrength.Preferred,
                                Extension = [extension]
                            }
                        }
                    ]
                }
            };

            // Use the simplified pattern from Rob's test - recreate his exact structure
            var packageServer = "https://packages.simplifier.net";
            string[] packageNames =
                [
                    "hl7.fhir.r4.core@4.0.1", 
                    "hl7.fhir.r4.expansions@4.0.1", 
                    "hl7.fhir.uv.extensions.r4@5.2.0",
                    "hl7.fhir.uv.tools.r4@0.3.0"
                ];
            var packageResolver = new FhirPackageSource(M.ModelInfo.ModelInspector, packageServer, packageNames);
            
            // Just use the package resolver directly for simplicity - the additional-binding extension should be in the packages
            var terminologyService = new LocalTerminologyService(packageResolver.AsAsync());
            var validator = new Validator(packageResolver.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(sd);

            // Assert - When extension is empty, should report missing mandatory slices with slice names
            if (emptyExtension)
            {
                var purposeIssue = outcome.Issue.FirstOrDefault(x => x.Details.Text.Contains("for slice purpose"));
                var valueSetIssue = outcome.Issue.FirstOrDefault(x => x.Details.Text.Contains("for slice valueSet"));

                purposeIssue.Should().NotBeNull("Should report missing purpose slice with slice name in message");
                valueSetIssue.Should().NotBeNull("Should report missing valueSet slice with slice name in message");
            }
            else
            {
                // When extension has data but is missing mandatory slices, should still report with slice names
                outcome.Success.Should().BeFalse("Extension with partial data should still fail validation for missing mandatory slices");
            }
        }
    }
}