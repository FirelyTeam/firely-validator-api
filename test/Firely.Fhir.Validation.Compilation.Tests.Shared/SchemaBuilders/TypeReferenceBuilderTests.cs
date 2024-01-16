/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using FluentAssertions;
using FluentAssertions.Primitives;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal static class SchemaFluentAssertionsExtensions
    {
        public static AndConstraint<ObjectAssertions> BeASchemaAssertionFor(this ObjectAssertions me, string uri) =>
                me.BeOfType<SchemaReferenceValidator>().Which
                .SchemaUri.Should().Be((Canonical)uri);

        public static AndConstraint<ObjectAssertions> BeASchemaAssertionFor(this ObjectAssertions me, Canonical c) =>
        me.BeOfType<SchemaReferenceValidator>().Which.SchemaUri.Should().Be(c);

        public static AndConstraint<ObjectAssertions> BeASchemaAssertionFor(this ObjectAssertions me, SchemaReferenceValidator other) =>
            me.BeOfType<SchemaReferenceValidator>().Which.SchemaUri.Should().Be(other.SchemaUri);


        public static AndConstraint<EnumAssertions<ValidationResult>> BeAFailureResult(this ObjectAssertions me) =>
                me.BeAssignableTo<IFixedResult>().Which.FixedResult.Should().Be(ValidationResult.Failure);
    }

    public class TypeReferenceBuilderTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public TypeReferenceBuilderTests(SchemaBuilderFixture fixture) => _fixture = fixture;

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

            var sa = sch.Should().BeOfType<AnyValidator>().Subject;
            sa.Members.Should().HaveCount(2);

            sa.Members[0].Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDORG1);
            sa.Members[1].Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDORG2);
            sa.SummaryError.Should().BeAFailureResult();
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
                new ReferencedInstanceValidator(SchemaReferenceValidator.ForResource),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void SupportsR3StylePrimitiveTypeRefs()
        {
            ElementDefinition.TypeRefComponent rc = new();
            var ce = new FhirUri();
            ce.SetStringExtension(CommonTypeRefComponentExtensions.SDXMLTYPEEXTENSION, "xsd:token");
            rc.CodeElement = ce;

            var converted = convertTypeReference(rc);
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
                new ReferencedInstanceValidator(new SchemaReferenceValidator(TestProfileArtifactSource.PROFILEDPROCEDURE)),
                options => options.IncludingAllRuntimeProperties());
        }

        [Fact]
        public void AggregationConstraintsForReferenceShouldBeGenerated()
        {
            var tr = build("Reference", targets: new[] { TestProfileArtifactSource.PROFILEDPROCEDURE });
            tr.AggregationElement.Add(new Code<ElementDefinition.AggregationMode>(ElementDefinition.AggregationMode.Bundled));
            tr.Versioning = ElementDefinition.ReferenceVersionRules.Independent;

            var sch = convertTypeReference(tr);
            var rr = sch.Should().BeOfType<AllValidator>().Subject
                .Members[1].Should().BeOfType<ReferencedInstanceValidator>().Subject;

            rr.VersioningRules.Should().Be(ReferenceVersionRules.Independent);
            rr.AggregationRules.Should().ContainInOrder(AggregationMode.Bundled);
        }

        [Fact]
        public void ExtensionTypeShouldUseDynamicSchemaReferenceValidator()
        {
            var sch = convert("Extension", profiles: new[] { TestProfileArtifactSource.PROFILEDORG1 });
            sch.Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDORG1);
        }

        [Fact]
        public void NakedContainedResourceShouldHaveReferenceValidationAgainstRTT()
        {
            var sch = convert("Resource");
            sch.Should().BeASchemaAssertionFor(SchemaReferenceValidator.ForResource);
        }

        [Fact]
        public void ContainedResourceShouldHaveReferenceValidationAgainstProfiles()
        {
            var sch = convert("Resource", profiles: new[] { TestProfileArtifactSource.PROFILEDPROCEDURE });
            sch.Should().BeASchemaAssertionFor(TestProfileArtifactSource.PROFILEDPROCEDURE);
        }

        [Fact]
        public void ReferenceOfAnyShouldBuildRTTSchema()
        {
            // This is how a Reference(Any) is encoded in a TypeReference.
            // This should use the runtime type of the target to validate against.
            var sch = convert("Reference", targets: new[] { "http://hl7.org/fhir/StructureDefinition/Resource" });
            var all = sch.Should().BeOfType<AllValidator>().Subject;

            all.Members[1].Should().BeOfType<ReferencedInstanceValidator>()
                .Which.Schema.Should().BeASchemaAssertionFor(SchemaReferenceValidator.ForResource);
        }

        [Fact]
        public void ContainedResourceWithAnyShouldBuildRTTSchema()
        {
            var resourceUri = "http://hl7.org/fhir/StructureDefinition/Resource";
            // Although this is probably not used, the situation is comparable
            // to a Reference(Any), where the type is "Resource" and the profile is
            // Resource too. This should use the runtime type of the target to validate against.
            var sch = convert("Resource", profiles: new[] { resourceUri });
            sch.Should().BeASchemaAssertionFor(resourceUri);
        }

        private static ElementDefinition.TypeRefComponent build(string code, string[]? profiles = null, string[]? targets = null)
#if STU3
            => new() { Code = code, Profile = profiles?.SingleOrDefault(), TargetProfile = targets?.SingleOrDefault() };
#else
            => new() { Code = code, Profile = profiles, TargetProfile = targets };
#endif

#if STU3
        private IAssertion? convert(string code, string[]? profiles = null, string[]? targets = null)
        {
            IEnumerable<ElementDefinition.TypeRefComponent> typeRefs = profiles switch
            {
                null when targets is not null => targets.Select(t => new ElementDefinition.TypeRefComponent() { Code = code, TargetProfile = t }),
                not null when targets is null => profiles.Select(p => new ElementDefinition.TypeRefComponent() { Code = code, Profile = p }),
                not null when targets is not null => cartesianProduct(profiles, targets).Select(p => new ElementDefinition.TypeRefComponent() { Code = code, Profile = p.Item1, TargetProfile = p.Item2 }),
                _ => new[] { new ElementDefinition.TypeRefComponent() { Code = code } }
            };

            return convert(typeRefs);

            static IEnumerable<(string, string)> cartesianProduct(string[] left, string[] right) =>
                left.Join(right, x => true, y => true, (l, r) => (l, r));
        }
#else
        private IAssertion? convert(string code, string[]? profiles = null, string[]? targets = null)
             => convertTypeReference(build(code, profiles, targets));
#endif

        private IAssertion? convert(IEnumerable<ElementDefinition.TypeRefComponent> trs) =>
            new TypeReferenceBuilder(_fixture.ResourceResolver).ConvertTypeReferences(trs);

        private IAssertion? convertTypeReference(ElementDefinition.TypeRefComponent typeRef)
            => new TypeReferenceBuilder(_fixture.ResourceResolver).ConvertTypeReference(CommonTypeRefComponent.Convert(typeRef));
    }
}

