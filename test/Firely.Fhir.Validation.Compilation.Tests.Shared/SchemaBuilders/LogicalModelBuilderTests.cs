/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    /// <summary>
    /// Tests for the builders handling the hl7.fhir.uv.tools marker extensions used by logical models,
    /// including the type-reference substitutions they trigger.
    /// </summary>
    public class LogicalModelBuilderTests : IClassFixture<SchemaBuilderFixture>
    {
        private const string TOOLS = "http://hl7.org/fhir/tools/StructureDefinition/";
        private const string TYPE_SPECIFIER = TOOLS + "type-specifier";
        private const string IMPLIED_STRING_PREFIX = TOOLS + "implied-string-prefix";
        private const string JSON_NULLABLE = TOOLS + "json-nullable";
        private const string ID_EXPECTATION = TOOLS + "id-expectation";

        private readonly SchemaBuilderFixture _fixture;

        public LogicalModelBuilderTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        private static ElementDefinition uuidElement(params Extension[] extensions)
        {
            var def = new ElementDefinition("Model.field");
            def.Type.Add(new ElementDefinition.TypeRefComponent { Code = "uuid" });
            def.Extension.AddRange(extensions);
            return def;
        }

        private static ElementDefinitionNavigator navFor(ElementDefinition def)
        {
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();
            return nav;
        }

        private static Extension typeSpecifier(string? condition, string? type)
        {
            var ext = new Extension { Url = TYPE_SPECIFIER };
            if (condition is not null) ext.Extension.Add(new Extension("condition", new FhirString(condition)));
            if (type is not null) ext.Extension.Add(new Extension("type", new FhirUri(type)));
            return ext;
        }

        private IEnumerable<IAssertion> buildTypeReference(ElementDefinition def) =>
            new TypeReferenceBuilder(_fixture.ResourceResolver).Build(navFor(def), ElementConversionMode.Full);

        // --- type-specifier -----------------------------------------------------------------------

        [Fact]
        public void WellFormedTypeSpecifierIsBuiltAndReplacesTheTypeReference()
        {
            var def = uuidElement(typeSpecifier("%resource.hook = 'order-sign'", "http://example.org/Ctx"));

            var validator = new TypeSpecifierBuilder().Build(navFor(def), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<TypeSpecifierValidator>().Subject;

            validator.Cases.Should().ContainSingle();
            validator.Cases[0].Condition.Should().Be("%resource.hook = 'order-sign'");
            validator.Cases[0].Type.Should().Be((Canonical)"http://example.org/Ctx");

            // The type-specifier picks the type at runtime, so the declared type[] is not validated.
            buildTypeReference(def).Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, "http://example.org/Ctx")] // no condition
        [InlineData("%resource.hook = 'x'", null)]   // no type
        [InlineData(null, null)]                     // empty marker
        public void MalformedTypeSpecifierDoesNotSilentlyDisableTypeValidation(string? condition, string? type)
        {
            var def = uuidElement(typeSpecifier(condition, type));

            new TypeSpecifierBuilder().Build(navFor(def), ElementConversionMode.Full).Should().BeEmpty();

            // No replacement assertion was built, so the declared type reference must remain in place.
            buildTypeReference(def).Should().ContainSingle().Subject
                .Should().BeASchemaAssertionFor(Canonical.ForCoreType("uuid"));
        }

        [Fact]
        public void NoTypeSpecifierBuildsNothing()
        {
            new TypeSpecifierBuilder().Build(navFor(uuidElement()), ElementConversionMode.Full).Should().BeEmpty();
        }

        [Fact]
        public void TypeSpecifierIsNotBuiltForContentReferences()
        {
            var def = uuidElement(typeSpecifier("true", "http://example.org/Ctx"));
            new TypeSpecifierBuilder().Build(navFor(def), ElementConversionMode.ContentReference).Should().BeEmpty();
        }

        // --- implied-string-prefix ----------------------------------------------------------------

        [Fact]
        public void ImpliedStringPrefixIsBuiltAndReplacesTheTypeReference()
        {
            var def = uuidElement(new Extension(IMPLIED_STRING_PREFIX, new FhirString("urn:uuid:")));

            var validator = new ImpliedStringPrefixBuilder().Build(navFor(def), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<ImpliedStringPrefixValidator>().Subject;

            validator.Prefix.Should().Be("urn:uuid:");
            // The declared type's own schema is re-run against "prefix + value" by the validator itself.
            validator.SchemaUri.Should().Be(Canonical.ForCoreType("uuid"));

            buildTypeReference(def).Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        public void MalformedImpliedStringPrefixDoesNotSilentlyDisableTypeValidation(string prefix)
        {
            var def = uuidElement(new Extension(IMPLIED_STRING_PREFIX, new FhirString(prefix)));

            new ImpliedStringPrefixBuilder().Build(navFor(def), ElementConversionMode.Full).Should().BeEmpty();
            buildTypeReference(def).Should().ContainSingle().Subject
                .Should().BeASchemaAssertionFor(Canonical.ForCoreType("uuid"));
        }

        [Fact]
        public void ImpliedStringPrefixWithoutValueDoesNotSilentlyDisableTypeValidation()
        {
            var def = uuidElement(new Extension { Url = IMPLIED_STRING_PREFIX });

            new ImpliedStringPrefixBuilder().Build(navFor(def), ElementConversionMode.Full).Should().BeEmpty();
            buildTypeReference(def).Should().ContainSingle().Subject
                .Should().BeASchemaAssertionFor(Canonical.ForCoreType("uuid"));
        }

        [Fact]
        public void NoImpliedStringPrefixBuildsNothing()
        {
            new ImpliedStringPrefixBuilder().Build(navFor(uuidElement()), ElementConversionMode.Full).Should().BeEmpty();
        }

        // --- json-nullable ------------------------------------------------------------------------

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void JsonNullableIsBuilt(bool isNullable)
        {
            var def = uuidElement(new Extension(JSON_NULLABLE, new FhirBoolean(isNullable)));

            new JsonNullableBuilder().Build(navFor(def), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<JsonNullableValidator>()
                .Which.IsNullable.Should().Be(isNullable);
        }

        [Fact]
        public void MalformedOrAbsentJsonNullableBuildsNothing()
        {
            new JsonNullableBuilder().Build(navFor(uuidElement()), ElementConversionMode.Full).Should().BeEmpty();

            var malformed = uuidElement(new Extension(JSON_NULLABLE, new FhirString("yes")));
            new JsonNullableBuilder().Build(navFor(malformed), ElementConversionMode.Full).Should().BeEmpty();
        }

        // --- json-nullable and required members ---------------------------------------------------

        private static StructureDefinition model(StructureDefinition.StructureDefinitionKind kind, params ElementDefinition[] elements)
        {
            var sd = new StructureDefinition
            {
                Url = "http://example.org/Model",
                Name = "Model",
                Type = "http://example.org/Model",
                Kind = kind,
                Abstract = false,
                Derivation = StructureDefinition.TypeDerivationRule.Specialization,
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Element",
                Snapshot = new StructureDefinition.SnapshotComponent()
            };
            sd.Snapshot.Element.AddRange(elements);
            return sd;
        }

        private static ElementDefinition member(string path, int min, string max = "1", params Extension[] extensions)
        {
            var def = new ElementDefinition(path) { Min = min, Max = max };
            def.Type.Add(new ElementDefinition.TypeRefComponent { Code = "string" });
            def.Extension.AddRange(extensions);
            return def;
        }

        private List<IAssertion> convertElement(StructureDefinition sd, string? childPath = null)
        {
            var nav = ElementDefinitionNavigator.ForSnapshot(sd);
            nav.MoveToFirstChild();
            if (childPath is not null) Assert.True(nav.JumpToFirst(childPath));
            return _fixture.Builder.ConvertElement(nav);
        }

        private static IEnumerable<string> requiredMembersOf(List<IAssertion> members) =>
            members.OfType<RequiredValidator>().Should().ContainSingle().Subject.RequiredMembers;

        [Fact]
        public void ExplicitlyNonNullableRequiredMemberStaysRequired()
        {
            var sd = model(StructureDefinition.StructureDefinitionKind.Logical,
                new ElementDefinition("Model"),
                member("Model.nonNullable", 1, "1", new Extension(JSON_NULLABLE, new FhirBoolean(false))));

            requiredMembersOf(convertElement(sd)).Should().BeEquivalentTo(["nonNullable"]);
        }

        [Fact]
        public void JsonNullableIsIgnoredOutsideLogicalModels()
        {
            var sd = model(StructureDefinition.StructureDefinitionKind.ComplexType,
                new ElementDefinition("Model"),
                member("Model.nullable", 1, "1", new Extension(JSON_NULLABLE, new FhirBoolean(true))));

            requiredMembersOf(convertElement(sd)).Should().BeEquivalentTo(["nullable"]);
        }

        // --- json-property-key (keyed objects) ----------------------------------------------------

        [Fact]
        public void KeyedObjectCarriesTheElementsCardinalityForItsEntries()
        {
            const string JSON_PROPERTY_KEY = TOOLS + "json-property-key";

            var sd = model(StructureDefinition.StructureDefinitionKind.Logical,
                new ElementDefinition("Model"),
                new ElementDefinition("Model.prefetch")
                {
                    Min = 1,
                    Max = "*",
                    Extension = { new Extension(JSON_PROPERTY_KEY, new Code("key")) }
                },
                member("Model.prefetch.key", 1),
                member("Model.prefetch.value", 1));

            var members = convertElement(sd, "Model.prefetch");

            var keyed = members.OfType<KeyedObjectValidator>().Should().ContainSingle().Subject;
            keyed.Min.Should().Be(1);
            keyed.Max.Should().BeNull();

            // The element is a single JSON object, so counting its occurrences would be meaningless.
            members.OfType<CardinalityValidator>().Should().BeEmpty();
        }

        // --- id-expectation -----------------------------------------------------------------------

        [Theory]
        [InlineData("optional", IdExpectation.Optional)]
        [InlineData("required", IdExpectation.Required)]
        [InlineData("prohibited", IdExpectation.Prohibited)]
        public void IdExpectationIsBuilt(string code, IdExpectation expected)
        {
            var def = uuidElement(new Extension(ID_EXPECTATION, new Code(code)));

            new IdExpectationBuilder().Build(navFor(def), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<IdExpectationValidator>()
                .Which.Expectation.Should().Be(expected);
        }

        [Fact]
        public void MalformedOrAbsentIdExpectationBuildsNothing()
        {
            new IdExpectationBuilder().Build(navFor(uuidElement()), ElementConversionMode.Full).Should().BeEmpty();

            var malformed = uuidElement(new Extension(ID_EXPECTATION, new Code("whatever")));
            new IdExpectationBuilder().Build(navFor(malformed), ElementConversionMode.Full).Should().BeEmpty();
        }

        // --- CDS Hooks examples --------------------------------------------------------------------

        [Fact]
        public void CdsHooksContextExampleBuildsTypeSpecifierValidator()
        {
            var contextElement = new ElementDefinition("CdsHooksRequest.context")
            {
                Type = { new ElementDefinition.TypeRefComponent { Code = "Element" } },
                Extension =
                {
                    typeSpecifier("%resource.hook = 'order-sign'", "http://example.org/fhir/StructureDefinition/OrderSignContext")
                }
            };

            new TypeSpecifierBuilder().Build(navFor(contextElement), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<TypeSpecifierValidator>();
        }

        [Fact]
        public void CdsHooksHookInstanceExampleBuildsImpliedStringPrefixValidator()
        {
            var hookInstanceElement = new ElementDefinition("CdsHooksRequest.hookInstance")
            {
                Type = { new ElementDefinition.TypeRefComponent { Code = "uuid" } },
                Extension = { new Extension(IMPLIED_STRING_PREFIX, new FhirString("urn:uuid:")) }
            };

            new ImpliedStringPrefixBuilder().Build(navFor(hookInstanceElement), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<ImpliedStringPrefixValidator>();
        }

        [Fact]
        public void CdsHooksPrefetchItemExampleBuildsKeyedObjectWithEntryCardinality()
        {
            const string JSON_PROPERTY_KEY = TOOLS + "json-property-key";

            var sd = model(StructureDefinition.StructureDefinitionKind.Logical,
                new ElementDefinition("CdsHooksRequest"),
                new ElementDefinition("CdsHooksRequest.prefetch")
                {
                    Min = 1,
                    Max = "1"
                },
                new ElementDefinition("CdsHooksRequest.prefetch.item")
                {
                    Min = 1,
                    Max = "*",
                    Extension = { new Extension(JSON_PROPERTY_KEY, new Code("key")) }
                },
                member("CdsHooksRequest.prefetch.item.key", 1),
                member("CdsHooksRequest.prefetch.item.value", 1));

            var members = convertElement(sd, "CdsHooksRequest.prefetch.item");
            var keyed = members.OfType<KeyedObjectValidator>().Should().ContainSingle().Subject;
            keyed.Min.Should().Be(1);
            keyed.Max.Should().BeNull();
        }

        [Fact]
        public void CdsHooksDraftOrdersExampleBuildsJsonNullableValidator()
        {
            var draftOrdersElement = new ElementDefinition("CdsHooksResponse.cards.suggestion.draftOrders")
            {
                Type = { new ElementDefinition.TypeRefComponent { Code = "string" } },
                Extension = { new Extension(JSON_NULLABLE, new FhirBoolean(true)) }
            };

            new JsonNullableBuilder().Build(navFor(draftOrdersElement), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<JsonNullableValidator>()
                .Which.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void CdsHooksCardIdExampleBuildsIdExpectationValidator()
        {
            var cardIdElement = new ElementDefinition("CdsHooksResponse.cards.id")
            {
                Type = { new ElementDefinition.TypeRefComponent { Code = "uuid" } },
                Extension = { new Extension(ID_EXPECTATION, new Code("required")) }
            };

            new IdExpectationBuilder().Build(navFor(cardIdElement), ElementConversionMode.Full)
                .Should().ContainSingle().Subject.Should().BeOfType<IdExpectationValidator>()
                .Which.Expectation.Should().Be(IdExpectation.Required);
        }
    }
}
