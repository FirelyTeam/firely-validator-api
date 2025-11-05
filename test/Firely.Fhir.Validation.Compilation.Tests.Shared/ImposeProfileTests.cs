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
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class ImposeProfileTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public ImposeProfileTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        [Fact]
        public void StructureDefinitionWithImposeProfile_ExtractsImposedProfiles()
        {
            // Create a StructureDefinition with imposeProfile extension
            var sd = new StructureDefinition
            {
                Url = "http://example.org/StructureDefinition/TestPatientProfile",
                Type = "Patient",
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Patient",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Extension = new List<Extension>
                {
                    new Extension
                    {
                        Url = "http://hl7.org/fhir/StructureDefinition/structuredefinition-imposeProfile",
#if R4 || R4B || R5
                        Value = new Canonical("http://example.org/StructureDefinition/ImposedProfile1")
#else
                        Value = new FhirUri("http://example.org/StructureDefinition/ImposedProfile1")
#endif
                    },
                    new Extension
                    {
                        Url = "http://hl7.org/fhir/StructureDefinition/structuredefinition-imposeProfile",
#if R4 || R4B || R5
                        Value = new Canonical("http://example.org/StructureDefinition/ImposedProfile2")
#else
                        Value = new FhirUri("http://example.org/StructureDefinition/ImposedProfile2")
#endif
                    }
                },
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element = new List<ElementDefinition>
                    {
                        new ElementDefinition
                        {
                            ElementId = "Patient",
                            Path = "Patient"
                        }
                    }
                }
            };

            // Create a SchemaBuilder with a resolver that includes the test SD
            var resolver = new InMemoryResourceResolver();
            resolver.Add(sd);
            
            // Add the base Patient StructureDefinition (using the fixture's resolver)
            var basePatient = TaskHelper.Await(() => _fixture.Source.FindStructureDefinitionAsync("http://hl7.org/fhir/StructureDefinition/Patient"));
            if (basePatient != null)
                resolver.Add(basePatient);

            var builder = new SchemaBuilder(resolver);
            var schema = builder.Build(sd.ToTypedElement().ToElementNavigator(), ElementConversionMode.Full).Single() as ResourceSchema;

            // Verify that the imposed profiles were extracted
            schema.Should().NotBeNull();
            schema!.StructureDefinition.ImposeProfiles.Should().NotBeNull();
            schema.StructureDefinition.ImposeProfiles.Should().HaveCount(2);
            schema.StructureDefinition.ImposeProfiles.Should().Contain(new Canonical("http://example.org/StructureDefinition/ImposedProfile1"));
            schema.StructureDefinition.ImposeProfiles.Should().Contain(new Canonical("http://example.org/StructureDefinition/ImposedProfile2"));
        }

        [Fact]
        public void StructureDefinitionWithoutImposeProfile_HasNullImposedProfiles()
        {
            // Get a standard Patient schema from the fixture
            var schema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient") as ResourceSchema;

            // Verify that a standard profile has no imposed profiles
            schema.Should().NotBeNull();
            schema!.StructureDefinition.ImposeProfiles.Should().BeNull();
        }

        [Fact]
        public void ValidationAgainstProfileWithImposeProfile_ValidatesImposedConstraints()
        {
            // Create an imposed profile with a constraint (patient must have a name)
            var imposedProfile = new StructureDefinition
            {
                Url = "http://example.org/StructureDefinition/PatientWithName",
                Type = "Patient",
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Patient",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element = new List<ElementDefinition>
                    {
                        new ElementDefinition
                        {
                            ElementId = "Patient",
                            Path = "Patient"
                        },
                        new ElementDefinition
                        {
                            ElementId = "Patient.name",
                            Path = "Patient.name",
                            Min = 1 // Require at least one name
                        }
                    }
                }
            };

            // Create a main profile that imposes the PatientWithName profile
            var mainProfile = new StructureDefinition
            {
                Url = "http://example.org/StructureDefinition/MyPatientProfile",
                Type = "Patient",
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Patient",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Extension = new List<Extension>
                {
                    new Extension
                    {
                        Url = "http://hl7.org/fhir/StructureDefinition/structuredefinition-imposeProfile",
#if R4 || R4B || R5
                        Value = new Canonical("http://example.org/StructureDefinition/PatientWithName")
#else
                        Value = new FhirUri("http://example.org/StructureDefinition/PatientWithName")
#endif
                    }
                },
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element = new List<ElementDefinition>
                    {
                        new ElementDefinition
                        {
                            ElementId = "Patient",
                            Path = "Patient"
                        }
                    }
                }
            };

            // Set up resolver with all required profiles
            var resolver = new InMemoryResourceResolver();
            resolver.Add(imposedProfile);
            resolver.Add(mainProfile);
            
            var basePatient = TaskHelper.Await(() => _fixture.Source.FindStructureDefinitionAsync("http://hl7.org/fhir/StructureDefinition/Patient"));
            if (basePatient != null)
            {
                resolver.Add(basePatient);
            }

            // Build schemas
            var builder = new SchemaBuilder(resolver);
            var imposedSchema = builder.Build(imposedProfile.ToTypedElement().ToElementNavigator(), ElementConversionMode.Full).Single();
            var mainSchema = builder.Build(mainProfile.ToTypedElement().ToElementNavigator(), ElementConversionMode.Full).Single();
            
            // Create a combined resolver that includes both fixture and our test profiles
            var combinedResolver = new CombinedSchemaResolver(_fixture.SchemaResolver, imposedSchema, mainSchema);
            var settings = _fixture.NewValidationSettings();
            settings.ElementSchemaResolver = combinedResolver;

            // Test case 1: Patient without name should fail validation against the main profile
            // because the imposed profile requires a name
            var patientWithoutName = new Patient { Id = "test1" }.ToPocoNode();
            var result = mainSchema.Validate(patientWithoutName, settings);
            
            result.IsSuccessful.Should().BeFalse("Patient without name should fail because imposed profile requires a name");
            result.Evidence.Should().Contain(e => e is IssueAssertion ia && ia.Message.Contains("name"));

            // Test case 2: Patient with name should pass validation
            var patientWithName = new Patient 
            { 
                Id = "test2",
                Name = new List<HumanName> { new HumanName { Family = "Doe" } }
            }.ToPocoNode();
            
            result = mainSchema.Validate(patientWithName, settings);
            result.IsSuccessful.Should().BeTrue("Patient with name should pass validation");
        }

        // Helper class for in-memory resource resolution
        private class InMemoryResourceResolver : IAsyncResourceResolver
        {
            private readonly Dictionary<string, Resource> _resources = new();

            public void Add(Resource resource)
            {
                if (resource is StructureDefinition sd && sd.Url != null)
                    _resources[sd.Url] = resource;
            }

            public System.Threading.Tasks.Task<Resource?> ResolveByCanonicalUriAsync(string uri)
            {
                _resources.TryGetValue(uri, out var resource);
                return System.Threading.Tasks.Task.FromResult(resource);
            }

            public System.Threading.Tasks.Task<Resource?> ResolveByUriAsync(string uri)
            {
                _resources.TryGetValue(uri, out var resource);
                return System.Threading.Tasks.Task.FromResult(resource);
            }
        }

        // Helper class to combine schema resolvers
        private class CombinedSchemaResolver : IElementSchemaResolver
        {
            private readonly IElementSchemaResolver _baseResolver;
            private readonly Dictionary<string, ElementSchema> _additionalSchemas = new();

            public CombinedSchemaResolver(IElementSchemaResolver baseResolver, params ElementSchema[] additionalSchemas)
            {
                _baseResolver = baseResolver;
                foreach (var schema in additionalSchemas)
                {
                    _additionalSchemas[schema.Id.ToString()] = schema;
                }
            }

            public ElementSchema? GetSchema(Canonical schemaUri)
            {
                var uriString = schemaUri.ToString();
                if (_additionalSchemas.TryGetValue(uriString, out var schema))
                    return schema;
                
                return _baseResolver.GetSchema(schemaUri);
            }
        }
    }
}
