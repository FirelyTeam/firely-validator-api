/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class BasicSchemaConverterTests : IClassFixture<SchemaConverterFixture>
    {
        private readonly SchemaConverterFixture _fixture;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ITestOutputHelper _output;
#pragma warning restore IDE0052 // I'd like to keep the output handy when I need it

#if STU3
        private readonly string _schemaSnapDirectory = "R3SchemaSnaps";
#else
        private readonly string _schemaSnapDirectory = "R4SchemaSnaps";
#endif

        public BasicSchemaConverterTests(SchemaConverterFixture fixture, ITestOutputHelper oh) =>
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
                //if (Path.GetFileName(file) != "ProfiledObservation.json") continue;

                var contents = File.ReadAllText(file);
                var expected = JObject.Parse(contents);

                var schemaUri = expected.Value<string>("id");
                var generated = _fixture.SchemaResolver.GetSchema(schemaUri!);
                var actualJson = generated!.ToJson().ToString();
                if (overwrite)
                {
                    File.WriteAllText(@"..\..\..\" + file, actualJson);
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

#if !STU3
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
}

