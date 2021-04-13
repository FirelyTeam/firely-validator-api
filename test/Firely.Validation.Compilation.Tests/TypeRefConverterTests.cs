/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using FluentAssertions.Primitives;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public static class SchemaFluentAssertionsExtensions
    {
        public static AndConstraint<ObjectAssertions> BeASchemaAssertionFor(this ObjectAssertions me, string uri) =>
                me.BeOfType<SchemaAssertion>().Which
                .SchemaUri.Should().Be(uri);

        public static AndConstraint<ObjectAssertions> BeAFailureResult(this ObjectAssertions me) =>
                me.BeOfType<ResultAssertion>().Which.Result.Should().Be(ValidationResult.Failure);
    }

    public class TypeRefConverterTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public TypeRefConverterTests(SchemaConverterFixture fixture) => _fixture = fixture;

        private const string HL7SDPREFIX = "http://hl7.org/fhir/StructureDefinition/";
        private const string REFERENCE_PROFILE = HL7SDPREFIX + "Reference";
        private const string CODE_PROFILE = HL7SDPREFIX + "Code";
        private const string IDENTIFIER_PROFILE = HL7SDPREFIX + "Identifier";
        private const string MYPROFILE1 = "http://example.org/myProfile";
        private const string MYPROFILE2 = "http://example.org/myProfile2";


        [Fact]
        public void TypRefProfileShouldResultInASingleSchemaAssertion()
        {
            var sch = convert("Identifier", profiles: new[] { MYPROFILE1 });
            sch.Should().BeASchemaAssertionFor(MYPROFILE1);
        }

        [Fact]
        public void TypRefWithMultipleProfilesShouldResultInASliceWithSchemaAssertions()
        {
            var sch = convert("Identifier", profiles: new[] { MYPROFILE1, MYPROFILE2 });

            var sa = sch.Should().BeOfType<SliceAssertion>().Subject;
            sa.Slices.Should().HaveCount(2);

            sa.Slices[0].Condition.Should().BeASchemaAssertionFor(MYPROFILE1);
            sa.Slices[0].Assertion.Should().BeOfType<ResultAssertion>().Which.Result.Should().Be(ValidationResult.Success);

            sa.Slices[1].Condition.Should().BeASchemaAssertionFor(MYPROFILE2);
            sa.Slices[1].Assertion.Should().BeOfType<ResultAssertion>().Which.Result.Should().Be(ValidationResult.Success);

            sa.Default.Should().BeAFailureResult();
        }

        [Fact]
        public void TypRefShouldHaveADefaultProfile()
        {
            var sch = convert("Identifier");
            sch.Should().BeASchemaAssertionFor(IDENTIFIER_PROFILE);
        }

        [Fact]
        public void MultipleTypRefsShouldResultInATypeSlice()
        {
            var sch = convert(new[] { build("Identifier"), build("Code") });

            var sa = sch.Should().BeOfType<SliceAssertion>().Subject;
            sa.Slices.Should().HaveCount(2);

            sa.Slices[0].Condition.Should().BeOfType<FhirTypeLabel>().Which.Label.Should().Be("Identifier");
            sa.Slices[0].Assertion.Should().BeASchemaAssertionFor(IDENTIFIER_PROFILE);
            sa.Slices[1].Condition.Should().BeOfType<FhirTypeLabel>().Which.Label.Should().Be("Code");
            sa.Slices[1].Assertion.Should().BeASchemaAssertionFor(CODE_PROFILE);

            sa.Default.Should().BeAFailureResult();
        }

        [Fact]
        public void NakedReferenceTypeShouldHaveReferenceValidationAgainstDefaults()
        {
            var sch = convert("Reference");
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(REFERENCE_PROFILE);
            all.Members[1].Should().BeEquivalentTo(
                new ResourceReferenceAssertion("reference",
                    new AllAssertion(
                        TypeReferenceConverter.FOR_RUNTIME_TYPE,
                        TypeReferenceConverter.META_PROFILE_ASSERTION)),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void ReferenceWithTargetProfilesShouldHaveReferenceValidationAgainstProfiles()
        {
            var sch = convert("Reference", targets: new[] { MYPROFILE1 });
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(REFERENCE_PROFILE);
            all.Members[1].Should().BeEquivalentTo(
                new ResourceReferenceAssertion("reference",
                    new AllAssertion(
                        new SchemaAssertion(new Uri(MYPROFILE1)),
                        TypeReferenceConverter.META_PROFILE_ASSERTION)),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void AggregationConstraintsForReferenceShouldBeGenerated()
        {
            var tr = build("Reference", targets: new[] { MYPROFILE1 });
            tr.AggregationElement.Add(new Code<ElementDefinition.AggregationMode>(ElementDefinition.AggregationMode.Bundled));
            tr.Versioning = ElementDefinition.ReferenceVersionRules.Independent;

            var sch = TypeReferenceConverter.ConvertTypeReference(tr);
            var rr = sch.Should().BeOfType<AllAssertion>().Subject
                .Members[1].Should().BeOfType<ResourceReferenceAssertion>().Subject;

            rr.VersioningRules.Should().Be(ElementDefinition.ReferenceVersionRules.Independent);
            rr.AggregationRules.Should().ContainInOrder(ElementDefinition.AggregationMode.Bundled);
        }

        [Fact]
        public void ExtensionTypeShouldHaveReferenceValidationAgainstUrl()
        {
            var sch = convert("Extension", profiles: new[] { MYPROFILE2 });
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(MYPROFILE2);
            all.Members[1].Should().BeEquivalentTo(TypeReferenceConverter.URL_PROFILE_ASSERTION,
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void NakedContainedResourceShouldHaveReferenceValidationAgainstRTT()
        {
            var sch = convert("Resource");
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeEquivalentTo(TypeReferenceConverter.FOR_RUNTIME_TYPE,
                options => options.IncludingAllRuntimeProperties());
            all.Members[1].Should().BeEquivalentTo(TypeReferenceConverter.META_PROFILE_ASSERTION,
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void ContainedResourceShouldHaveReferenceValidationAgainstProfiles()
        {
            var sch = convert("Resource", profiles: new[] { MYPROFILE2 });
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(MYPROFILE2);
            all.Members[1].Should().BeEquivalentTo(TypeReferenceConverter.META_PROFILE_ASSERTION,
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void ReferenceOfAnyShouldBuildRTTSchema()
        {
            // This is how a Reference(Any) is encoded in a TypeReference.
            // This should use the runtime type of the target to validate against.
            var sch = convert("Reference", targets: new[] { "http://hl7.org/fhir/StructureDefinition/Resource" });
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            var referenceAll = all.Members[1].Should().BeOfType<ResourceReferenceAssertion>()
                .Which.Schema.Should().BeOfType<AllAssertion>().Subject;

            referenceAll.Members[0].Should().BeOfType<SchemaAssertion>()
                .Which.SchemaOrigin.Should().Be(SchemaAssertion.SchemaUriOrigin.RuntimeType);
        }

        [Fact]
        public void ContainedResourceWithAnyShouldBuildRTTSchema()
        {
            // Although this is probably not used, the situation is comparable
            // to a Reference(Any), where the type is "Resource" and the profile is
            // Resource too. This should use the runtime type of the target to validate against.
            var sch = convert("Resource", profiles: new[] { "http://hl7.org/fhir/StructureDefinition/Resource" });
            var all = sch.Should().BeOfType<AllAssertion>().Subject;

            all.Members[0].Should().BeOfType<SchemaAssertion>()
                .Which.SchemaOrigin.Should().Be(SchemaAssertion.SchemaUriOrigin.RuntimeType);
        }

        private static ElementDefinition.TypeRefComponent build(string code, string[]? profiles = null, string[]? targets = null)
         => new() { Code = code, Profile = profiles, TargetProfile = targets };

        private static IAssertion convert(string code, string[]? profiles = null, string[]? targets = null)
             => TypeReferenceConverter.ConvertTypeReference(build(code, profiles, targets));

        private static IAssertion convert(IEnumerable<ElementDefinition.TypeRefComponent> trs) =>
            TypeReferenceConverter.ConvertTypeReferences(trs);
    }
}

