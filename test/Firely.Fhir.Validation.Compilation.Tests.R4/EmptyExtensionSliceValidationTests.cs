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
using System.Collections.Generic;
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
            
            // Create a custom extension that mimics the additional-binding extension with proper slices
            var customExtension = new M.StructureDefinition
            {
                Url = "http://example.org/test/StructureDefinition/test-sliced-extension",
                Name = "TestSlicedExtension",
                Status = M.PublicationStatus.Draft,
                Kind = M.StructureDefinition.StructureDefinitionKind.ComplexType,
                Abstract = false,
                Type = "Extension",
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Extension",
                Derivation = M.StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new M.StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        // Extension root with slicing
                        new M.ElementDefinition("Extension")
                        {
                            ElementId = "Extension",
                            Slicing = new M.ElementDefinition.SlicingComponent
                            {
                                Discriminator = [new M.ElementDefinition.DiscriminatorComponent { Type = M.ElementDefinition.DiscriminatorType.Value, Path = "url" }],
                                Rules = M.ElementDefinition.SlicingRules.Closed
                            }
                        },
                        
                        // Purpose slice (mandatory)
                        new M.ElementDefinition("Extension")
                        {
                            ElementId = "Extension:purpose",
                            SliceName = "purpose",
                            Min = 1,
                            Max = "1"
                        },
                        new M.ElementDefinition("Extension.url")
                        {
                            ElementId = "Extension:purpose.url",
                            Fixed = new M.FhirUri("purpose")
                        },
                        new M.ElementDefinition("Extension.value[x]")
                        {
                            ElementId = "Extension:purpose.value[x]",
                            Type = [new M.ElementDefinition.TypeRefComponent { Code = "code" }]
                        },

                        // ValueSet slice (mandatory)
                        new M.ElementDefinition("Extension")
                        {
                            ElementId = "Extension:valueSet",
                            SliceName = "valueSet", 
                            Min = 1,
                            Max = "1"
                        },
                        new M.ElementDefinition("Extension.url")
                        {
                            ElementId = "Extension:valueSet.url",
                            Fixed = new M.FhirUri("valueSet")
                        },
                        new M.ElementDefinition("Extension.value[x]")
                        {
                            ElementId = "Extension:valueSet.value[x]",
                            Type = [new M.ElementDefinition.TypeRefComponent { Code = "canonical" }]
                        }
                    ]
                }
            };

            // Arrange - Create the test extension instance (empty or with partial data)
            var extension = new M.Extension
            {
                Url = "http://example.org/test/StructureDefinition/test-sliced-extension"
            };

            if (!emptyExtension)
                extension.Extension = [new M.Extension("key", new M.Id("Key"))]; // Optional data but missing mandatory slices

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
            
            // Create a simple in-memory resolver for our custom StructureDefinitions 
            var inMemoryResolver = new SimpleInMemoryResolver(sd, customExtension);
            
            // Use MultiResolver as in Rob's original test - this is crucial!
            var resolver = new MultiResolver(inMemoryResolver, packageResolver);
            
            var terminologyService = new LocalTerminologyService(resolver.AsAsync());
            var validator = new Validator(resolver.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(sd);

            // Assert - When extension is empty, should report missing mandatory slices with slice names
            if (emptyExtension)
            {
                var purposeIssue = outcome.Issue.FirstOrDefault(x => x.Details?.Text?.Contains("for slice purpose") == true);
                var valueSetIssue = outcome.Issue.FirstOrDefault(x => x.Details?.Text?.Contains("for slice valueSet") == true);

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

    // Simple in-memory resolver for testing
    internal class SimpleInMemoryResolver : IResourceResolver
    {
        private readonly List<M.StructureDefinition> _structureDefinitions;

        public SimpleInMemoryResolver(params M.StructureDefinition[] structureDefinitions)
        {
            _structureDefinitions = new List<M.StructureDefinition>(structureDefinitions);
        }

        public M.Resource? ResolveByCanonicalUri(string uri)
        {
            return _structureDefinitions.FirstOrDefault(sd => sd.Url == uri);
        }

        public M.Resource? ResolveByUri(string uri)
        {
            return ResolveByCanonicalUri(uri);
        }
    }
}