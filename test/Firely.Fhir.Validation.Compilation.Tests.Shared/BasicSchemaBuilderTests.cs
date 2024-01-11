/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class BasicSchemaBuilderTests : IClassFixture<SchemaBuilderFixture>
    {
        private readonly SchemaBuilderFixture _fixture;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ITestOutputHelper _output;
#pragma warning restore IDE0052 // I'd like to keep the output handy when I need it

        private readonly string _schemaSnapDirectory = "..\\..\\..\\SchemaSnaps";

        public BasicSchemaBuilderTests(SchemaBuilderFixture fixture, ITestOutputHelper oh) =>
            (_output, _fixture) = (oh, fixture);

        [Fact(Skip = "Only enable this when you want to rewrite the snaps to update them to a new correct situation")]
        //[Fact]
        public void OverwriteSchemaSnaps()
        {
            compareToSchemaSnaps(true);
        }

        [Fact]
        public void CompareToCorrectSchemaSnaps()
        {
            compareToSchemaSnaps(false);
        }

        private void compareToSchemaSnaps(bool overwrite)
        {
            var filenames = Directory.EnumerateFiles(_schemaSnapDirectory, "*.json");
            foreach (var file in filenames)
            {
                var contents = File.ReadAllText(file);
                var expected = JObject.Parse(contents);

                var schemaUri = expected.Value<string>("id");
                var generated = _fixture.SchemaResolver.GetSchema(schemaUri!);
                var actualJson = generated!.ToJson().ToString();
                if (overwrite)
                {
                    File.WriteAllText(file, actualJson);
                    continue;
                }

                var actual = JObject.Parse(actualJson);

                Assert.True(JToken.DeepEquals(expected, actual), file);
            }
        }

        [Fact]
        public void SubschemaIsCreatedForContentRefsOnly()
        {
            var questionnaire = _fixture.SchemaResolver.GetSchemaForCoreType("Questionnaire");
            assertRefersToItemBackbone(questionnaire!);

            var itemSchema = questionnaire!.Members.OfType<DefinitionsAssertion>().Should().ContainSingle()
                .Which.Schemas.Should().ContainSingle().Subject;
            itemSchema.Id.Should().Be((Canonical)"#Questionnaire.item");
            assertRefersToItemBackbone(itemSchema);

            // Subschemas should have no cardinality constraints, these are present
            // at the call site.
            itemSchema.Members.OfType<CardinalityValidator>().Should().BeEmpty();

            static void assertRefersToItemBackbone(ElementSchema s)
            {
                var itemSchema = s.Members.OfType<ChildrenValidator>().Single()["item"]
                    .Should().BeOfType<ElementSchema>().Subject;

                var schemaRef = itemSchema.Members.OfType<SchemaReferenceValidator>()
                .Should().ContainSingle().Subject;

                schemaRef.SchemaUri!.Uri.Should().Be("http://hl7.org/fhir/StructureDefinition/Questionnaire");
                schemaRef.SchemaUri!.Anchor.Should().Be("Questionnaire.item");
            }
        }

#if !STU3
        // TODO: we have to find a way to make this FHIR agnostic
        [Fact]
        public void InjectMetaProfileTest()
        {
            var schema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Bundle");

            var bundle = new Bundle
            {
                Type = Bundle.BundleType.Collection,
                Entry = new List<Bundle.EntryComponent> {
                    new Bundle.EntryComponent {
                        FullUrl = "https://example.com/group/1",
                        Resource = new Group {
#if R5
                            Membership = Group.GroupMembershipBasis.Definitional,
#else
                            Actual = true,
#endif
                            Type = Group.GroupType.Person
                        }
                    }
                }
            };

            var context = ValidationSettings.BuildMinimalContext(_fixture.ValidateCodeService, _fixture.SchemaResolver);
            context.SelectMetaProfiles = metaCallback;

            var result = schema!.Validate(bundle.ToTypedElement(), context);
            result.Result.Should().Be(ValidationResult.Failure);

            context.SelectMetaProfiles = null;
            result = schema!.Validate(bundle.ToTypedElement(), context);
            result.Result.Should().Be(ValidationResult.Success);

            static Canonical[] metaCallback(string location, Canonical[] originalUrl)
             => location == "Bundle.entry[0].resource[0]" ? new Canonical[] { "http://hl7.org/fhir/StructureDefinition/groupdefinition" } : Array.Empty<Canonical>();
        }
#endif

        [Fact]
        public void ValidateExtensionTest()
        {
            var schema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient");
            schema.Should().NotBeNull(because: "StructureDefinition of Patient should be present");

            Patient patient = new();
            patient.AddExtension("http://example.com/extension1", new FhirString("A string"));
            patient.AddExtension("http://example.com/extension2", new FhirString("Another string"));

            var context = ValidationSettings.BuildMinimalContext(_fixture.ValidateCodeService, _fixture.SchemaResolver);

            // Do not resolve the extension
            context.FollowExtensionUrl = buildCallback(ExtensionUrlHandling.DontResolve);
            var result = schema!.Validate(patient.ToTypedElement(), context);
            result.Warnings.Should().BeEmpty();
            result.Errors.Should().OnlyContain(e => e.IssueNumber == Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);
            result.Result.Should().Be(ValidationResult.Failure, because: "extension2 could not be found.");

            // Warn if missing
            context.FollowExtensionUrl = buildCallback(ExtensionUrlHandling.WarnIfMissing);
            result = schema!.Validate(patient.ToTypedElement(), context);
            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.UNAVAILABLE_REFERENCED_PROFILE_WARNING.Code);
            result.Errors.Should().OnlyContain(e => e.IssueNumber == Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);

            // Error if missing
            context.FollowExtensionUrl = buildCallback(ExtensionUrlHandling.ErrorIfMissing);
            result = schema!.Validate(patient.ToTypedElement(), context);
            result.Errors.Should().Contain(w => w.IssueNumber == Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);
            result.Warnings.Should().BeEmpty();

            // Default
            context.FollowExtensionUrl = null;
            result = schema!.Validate(patient.ToTypedElement(), context);
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().OnlyContain(e => e.IssueNumber == Issue.UNAVAILABLE_REFERENCED_PROFILE_WARNING.Code);

            static ExtensionUrlFollower buildCallback(ExtensionUrlHandling action)
                => (location, extensionUrl) => extensionUrl == "http://example.com/extension1" ? action : ExtensionUrlHandling.ErrorIfMissing;
        }

        [Fact]
        public void ReferencesToBackbonesHaveTheirOwnCardinalities()
        {
            var questionnaire = _fixture.SchemaResolver.GetSchema(TestProfileArtifactSource.PROFILEDBACKBONEANDCONTENTREF);

            // This item *contains* its constraints, since it does not have a contentRef
            var itemSchema = assertHasCardinality(questionnaire!, 1, 100);

            // This item refers to its constraints, since it has a contentRef
            _ = assertHasCardinality(itemSchema, 5, 10);

            static ElementSchema assertHasCardinality(ElementSchema s, int min, int? max)
            {
                var itemSchema = s.Members.OfType<ChildrenValidator>().Single()["item"]
                    .Should().BeOfType<ElementSchema>().Subject;

                var cardinality = itemSchema.Members.OfType<CardinalityValidator>()
                .Should().ContainSingle().Subject;

                cardinality.Min.Should().Be(min);
                cardinality.Max.Should().Be(max);

                return itemSchema;
            }
        }

        [Fact]
        public void UseSelfDefinedSchemaBuilderTest()
        {
            var resolver = new StructureDefinitionToElementSchemaResolver(_fixture.ResourceResolver, new ISchemaBuilder[] { new SelfDefinedBuilder() });
            var schema = resolver.GetSchemaForCoreType("boolean");
            schema.Should().NotBeNull();

            var assertions = flattenSchema(schema!);

            assertions.Should().ContainSingle(a => a is SelfDefinedValidator);
        }

        private static IEnumerable<IAssertion> flattenSchema(ElementSchema schema)
        {
            return flattenMembers(schema.Members);

            static IEnumerable<IAssertion> flattenMembers(IEnumerable<IAssertion> assertions) =>
                !assertions.Any()
                    ? Enumerable.Empty<IAssertion>()
                    : assertions
                        .Concat(flattenMembers(assertions.OfType<ElementSchema>().SelectMany(s => s.Members)))
                        .Concat(flattenMembers(assertions.OfType<ChildrenValidator>().SelectMany(s => s.ChildList.Values)));
        }


        [Theory]
        [MemberData(nameof(InvariantTestcases))]
        public async System.Threading.Tasks.Task invariantValidation(FHIRAllTypes type, string key, Base poco, bool expected)
        {
            var sd = await _fixture.ResourceResolver.FindStructureDefinitionForCoreTypeAsync(type);
            var expression = sd.Snapshot.Element
                .SelectMany(elem => elem.Constraint)
                .SingleOrDefault(ce => ce.Key == key)?.Expression;


            if (expression is not null)
                poco.Predicate(expression).Should().Be(expected);
        }

        public static IEnumerable<object[]> InvariantTestcases =>
        new List<object[]>
        {
            new object[] { FHIRAllTypes.Reference, "ref-1", new ResourceReference{ Display = "Only a display element" }, true },
            new object[] { FHIRAllTypes.ElementDefinition, "eld-19", new ElementDefinition { Path = ":.ContainingSpecialCharacters" }, false},
            new object[] { FHIRAllTypes.ElementDefinition, "eld-19", new ElementDefinition { Path = "NoSpecialCharacters" }, true },
            new object[] { FHIRAllTypes.ElementDefinition, "eld-20", new ElementDefinition { Path = "   leadingSpaces" }, false},
            new object[] { FHIRAllTypes.ElementDefinition, "eld-20", new ElementDefinition { Path = "NoSpaces.withADot" }, true },
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-0", new StructureDefinition { Name = " leadingSpaces" }, false },
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-0", new StructureDefinition { Name = "Name" }, true },
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-29", new StructureDefinition { Kind = StructureDefinition.StructureDefinitionKind.Resource, Derivation = StructureDefinition.TypeDerivationRule.Specialization, Differential = new (){ Element = new(){ new(){Path = "Patient", Min = 2} } } }, false },
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-29", new StructureDefinition { Kind = StructureDefinition.StructureDefinitionKind.Resource, Derivation = StructureDefinition.TypeDerivationRule.Specialization, Differential = new (){ Element = new(){ new(){Path = "Patient", Min = 1} } } }, true },

#if R4
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-24",
                    new StructureDefinition.SnapshotComponent
                        {
                            Element = new List<ElementDefinition> {
                                new ElementDefinition
                                {
                                    ElementId = "coderef.reference",
                                    Type = new List<ElementDefinition.TypeRefComponent>
                                           {
                                                new ElementDefinition.TypeRefComponent { Code = "Reference", TargetProfile = new[] { "http://example.com/profile" } }
                                           }
                                },
                                new ElementDefinition
                                {
                                    ElementId = "coderef",
                                    Type = new List<ElementDefinition.TypeRefComponent>
                                           {
                                                new ElementDefinition.TypeRefComponent { Code = "CodeableReference"}
                                           }
                                },
                             }
                    }, false },
            new object[] { FHIRAllTypes.StructureDefinition, "sdf-25",
                    new StructureDefinition.SnapshotComponent
                        {
                            Element = new List<ElementDefinition> {
                                new ElementDefinition
                                {
                                    ElementId = "coderef.concept",
                                    Type = new List<ElementDefinition.TypeRefComponent>
                                           {
                                                new ElementDefinition.TypeRefComponent { Code = "CodeableConcept" }
                                           },
                                    Binding = new ElementDefinition.ElementDefinitionBindingComponent { Description = "Just a description" }
                                },
                                new ElementDefinition
                                {
                                    ElementId = "coderef",
                                    Type = new List<ElementDefinition.TypeRefComponent>
                                           {
                                                new ElementDefinition.TypeRefComponent { Code = "CodeableReference"}
                                           }
                                },
                             }
                    }, false },
            new object[] { FHIRAllTypes.Questionnaire, "que-7",
                    new Questionnaire.EnableWhenComponent
                        {
                            Operator = Questionnaire.QuestionnaireItemOperator.Exists,
                            Answer = new FhirBoolean(true)
                    }, true },
#endif
        };
    }

    internal static class AvoidUriUseExtensions
    {
        public static ElementSchema? GetSchema(this IElementSchemaResolver resolver, string uri) =>
            resolver.GetSchema(uri);
        public static ElementSchema? GetSchemaForCoreType(this IElementSchemaResolver resolver, string typename) =>
            resolver.GetSchema("http://hl7.org/fhir/StructureDefinition/" + typename);

    }

    internal class SelfDefinedBuilder : ISchemaBuilder
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (nav.Path == "boolean.value")
                yield return new SelfDefinedValidator();
        }
    }

    internal class SelfDefinedValidator : IValidatable
    {
        public JToken ToJson() => new JProperty("selfdefined-validator");

        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
            => ResultReport.SUCCESS;
    }
}

