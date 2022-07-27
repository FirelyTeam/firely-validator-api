/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using FluentAssertions.Primitives;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public static class SchemaFluentAssertionsExtensions
    {
        public static AndConstraint<ObjectAssertions> BeASchemaAssertionFor(this ObjectAssertions me, string uri) =>
                me.BeOfType<SchemaReferenceValidator>().Which
                .SchemaUri.Should().Be((Canonical)uri);

        public static AndConstraint<ObjectAssertions> BeAFailureResult(this ObjectAssertions me) =>
                me.BeAssignableTo<IFixedResult>().Which.FixedResult.Should().Be(ValidationResult.Failure);
    }

    public class TypeRefConverterTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public TypeRefConverterTests(SchemaConverterFixture fixture) => _fixture = fixture;

        private const string HL7SDPREFIX = "http://hl7.org/fhir/StructureDefinition/";
        private const string REFERENCE_PROFILE = HL7SDPREFIX + "Reference";
        private const string CODE_PROFILE = HL7SDPREFIX + "Code";
        private const string IDENTIFIER_PROFILE = HL7SDPREFIX + "Identifier";


        [Fact]
        public void TypRefWithMultipleProfilesShouldResultInASliceWithSchemaAssertions()
        {
            // This doesnt make sense, but by having two profiles on the same type, we're not generating a typelabel slicer first, 
            // but immediately go through slicing based on profile
            var sch = convert("Identifier", profiles: new[] { TestProfileArtifactSource.PROFILEDORG1, TestProfileArtifactSource.PROFILEDORG2 });

            var sa = sch.Should().BeOfType<SliceValidator>().Subject;
            sa.Slices.Should().HaveCount(2);

            sa.Slices[0].Condition.Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDORG1);
            sa.Slices[0].Assertion.Should().BeAssignableTo<IFixedResult>().Which.FixedResult.Should().Be(ValidationResult.Success);

            sa.Slices[1].Condition.Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDORG2);
            sa.Slices[1].Assertion.Should().BeAssignableTo<IFixedResult>().Which.FixedResult.Should().Be(ValidationResult.Success);

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

            var sa = sch.Should().BeOfType<SliceValidator>().Subject;
            sa.Slices.Should().HaveCount(2);

            sa.Slices[0].Condition.Should().BeOfType<FhirTypeLabelValidator>().Which.Label.Should().Be("Identifier");
            sa.Slices[0].Assertion.Should().BeASchemaAssertionFor(IDENTIFIER_PROFILE);
            sa.Slices[1].Condition.Should().BeOfType<FhirTypeLabelValidator>().Which.Label.Should().Be("Code");
            sa.Slices[1].Assertion.Should().BeASchemaAssertionFor(CODE_PROFILE);

            sa.Default.Should().BeAFailureResult();
        }

        [Fact]
        public void NakedReferenceTypeShouldHaveReferenceValidationAgainstDefaults()
        {
            var sch = convert("Reference");
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(REFERENCE_PROFILE);
            all.Members[1].Should().BeEquivalentTo(
                new ReferencedInstanceValidator("reference",
                    new AllValidator(
                        SchemaReferenceValidator.ForResource,
                        TypeReferenceConverter.META_PROFILE_ASSERTION)),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void SupportsR3StylePrimitiveTypeRefs()
        {
            ElementDefinition.TypeRefComponent rc = new();
            var ce = new FhirUri();
            ce.SetStringExtension(TypeReferenceConverter.SDXMLTYPEEXTENSION, "xsd:token");
            rc.CodeElement = ce;

            var converted = new TypeReferenceConverter(_fixture.ResourceResolver).ConvertTypeReference(rc);
            converted.Should().BeOfType<SchemaReferenceValidator>().Which.SchemaUri.
                Should().Be(new Canonical("http://hl7.org/fhirpath/System.String"));
        }


        [Fact]
        public void ReferenceWithTargetProfilesShouldHaveReferenceValidationAgainstProfiles()
        {
            var sch = convert("Reference", targets: new[] { TestProfileArtifactSource.PROFILEDPROCEDURE });
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(REFERENCE_PROFILE);
            all.Members[1].Should().BeEquivalentTo(
                new ReferencedInstanceValidator("reference",
                    new AllValidator(
                        new SchemaReferenceValidator(TestProfileArtifactSource.PROFILEDPROCEDURE),
                        TypeReferenceConverter.META_PROFILE_ASSERTION)),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void AggregationConstraintsForReferenceShouldBeGenerated()
        {
            var tr = build("Reference", targets: new[] { TestProfileArtifactSource.PROFILEDPROCEDURE });
            tr.AggregationElement.Add(new Code<ElementDefinition.AggregationMode>(ElementDefinition.AggregationMode.Bundled));
            tr.Versioning = ElementDefinition.ReferenceVersionRules.Independent;

            var sch = new TypeReferenceConverter(_fixture.ResourceResolver).ConvertTypeReference(tr);
            var rr = sch.Should().BeOfType<AllValidator>().Subject
                .Members[1].Should().BeOfType<ReferencedInstanceValidator>().Subject;

            rr.VersioningRules.Should().Be(ElementDefinition.ReferenceVersionRules.Independent);
            rr.AggregationRules.Should().ContainInOrder(ElementDefinition.AggregationMode.Bundled);
        }

        [Fact]
        public void ExtensionTypeShouldUseDynamicSchemaReferenceValidator()
        {
            var sch = convert("Extension", profiles: new[] { TestProfileArtifactSource.PROFILEDORG1 });
            var dyna = sch.Should().BeOfType<DynamicSchemaReferenceValidator>().Subject;

            dyna.SchemaUriMember.Should().Be("url");
            dyna.AdditionalSchemas.Should().BeEquivalentTo(new[] { new Canonical(TestProfileArtifactSource.PROFILEDORG1) });
            dyna.DefaultSchema.Should().NotBeNull().And.Subject.ToString().Should().Be("http://hl7.org/fhir/StructureDefinition/Extension");
        }

        [Fact]
        public void NakedContainedResourceShouldHaveReferenceValidationAgainstRTT()
        {
            var sch = convert("Resource");
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeEquivalentTo(SchemaReferenceValidator.ForResource,
                options => options.IncludingAllRuntimeProperties());
            all.Members[1].Should().BeEquivalentTo(TypeReferenceConverter.META_PROFILE_ASSERTION,
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void ContainedResourceShouldHaveReferenceValidationAgainstProfiles()
        {
            var sch = convert("Resource", profiles: new[] { TestProfileArtifactSource.PROFILEDPROCEDURE });
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members.Should().HaveCount(2);
            all.Members[0].Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDPROCEDURE);
            all.Members[1].Should().BeEquivalentTo(TypeReferenceConverter.META_PROFILE_ASSERTION,
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void ReferenceOfAnyShouldBuildRTTSchema()
        {
            // This is how a Reference(Any) is encoded in a TypeReference.
            // This should use the runtime type of the target to validate against.
            var sch = convert("Reference", targets: new[] { "http://hl7.org/fhir/StructureDefinition/Resource" });
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            var referenceAll = all.Members[1].Should().BeOfType<ReferencedInstanceValidator>()
                .Which.Schema.Should().BeOfType<AllValidator>().Subject;

            referenceAll.Members[0].Should().BeOfType<SchemaReferenceValidator>().Which.SchemaUri.Should().Be(Canonical.ForCoreType("Resource"));
        }

        [Fact]
        public void ContainedResourceWithAnyShouldBuildRTTSchema()
        {
            var resourceUri = "http://hl7.org/fhir/StructureDefinition/Resource";
            // Although this is probably not used, the situation is comparable
            // to a Reference(Any), where the type is "Resource" and the profile is
            // Resource too. This should use the runtime type of the target to validate against.
            var sch = convert("Resource", profiles: new[] { resourceUri });
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members[0].Should().BeOfType<SchemaReferenceValidator>().Which.SchemaUri.ToString().Should().Be(resourceUri);
        }

        private static ElementDefinition.TypeRefComponent build(string code, string[]? profiles = null, string[]? targets = null)
         => new() { Code = code, Profile = profiles, TargetProfile = targets };

        private IAssertion convert(string code, string[]? profiles = null, string[]? targets = null)
             => new TypeReferenceConverter(_fixture.ResourceResolver).ConvertTypeReference(build(code, profiles, targets));

        private IAssertion convert(IEnumerable<ElementDefinition.TypeRefComponent> trs) =>
            new TypeReferenceConverter(_fixture.ResourceResolver).ConvertTypeReferences(trs);
    }
}

