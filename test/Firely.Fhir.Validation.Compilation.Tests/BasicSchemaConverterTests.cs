/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Newtonsoft.Json.Linq;
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

        public BasicSchemaConverterTests(SchemaConverterFixture fixture, ITestOutputHelper oh) =>
            (_output, _fixture) = (oh, fixture);

        [Fact]
        public void CompareToCorrectSchemaSnaps()
        {
            // Set this to the filename (e.g. boolean.json) or wildcard ("*") to overwrite it with the newly generated output.
            string overwrite = "";

            var filenames = Directory.EnumerateFiles("SchemaSnaps", "*.json");
            foreach (var file in filenames)
            {
                //if (Path.GetFileName(file) != "ProfiledObservation.json") continue;

                var contents = File.ReadAllText(file);
                var expected = JObject.Parse(contents);

                var schemaUri = expected.Value<string>("id");
                var generated = _fixture.SchemaResolver.GetSchema(schemaUri!);
                var actualJson = generated!.ToJson().ToString();
                if (Path.GetFileName(file) == overwrite || overwrite == "*")
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
    }

    internal static class AvoidUriUseExtensions
    {
        public static ElementSchema? GetSchema(this IElementSchemaResolver resolver, string uri) =>
            resolver.GetSchema(uri);
        public static ElementSchema? GetSchemaForCoreType(this IElementSchemaResolver resolver, string typename) =>
            resolver.GetSchema("http://hl7.org/fhir/StructureDefinition/" + typename);

    }
}

