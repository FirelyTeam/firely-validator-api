/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using FluentAssertions.Equivalency;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation.Tests
{

    public class SlicingSchemaConverterTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public SlicingSchemaConverterTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        private async T.Task<List<IAssertion>> createElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            return _fixture.Builder.ConvertElement(sdNav);
        }

        private async T.Task<SliceValidator> createSliceForElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            var slicev = _fixture.Builder.CreateSliceValidator(sdNav);
            return (SliceValidator)slicev;
        }

        private readonly IAssertion _sliceClosedAssertion =
                  new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE,
                      "Element does not match any slice and the group is closed.");

        private readonly SliceValidator.SliceCase _fixedSlice = new("Fixed",
                    new PathSelectorValidator("system", new FixedValidator(new FhirUri("http://example.com/some-bsn-uri"))),
                    new ElementSchema("#Patient.identifier:Fixed"));

        private static SliceValidator.SliceCase getPatternSlice(string profile) =>
            new("PatternBinding",
                    new PathSelectorValidator("system", new AllValidator(shortcircuitEvaluation: true,
                        new PatternValidator(new FhirUri("http://example.com/someuri")),
                        new BindingValidator("http://example.com/demobinding", strength: BindingValidator.BindingStrength.Required,
                                             context: $"{profile}#Patient.identifier.system"))),
                    new ElementSchema("#Patient.identifier:PatternBinding"));

        [Fact]
        public async T.Task TestValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASE, "Patient.identifier");

            // This is a *closed* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion, _fixedSlice,
                getPatternSlice(TestProfileArtifactSource.VALUESLICETESTCASE));

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx))
                .UsingCanonicalCompare());
        }

        private static bool excludeSliceAssertionCheck(IMemberInfo memberInfo) =>
            Regex.IsMatch(memberInfo.Path, @"Slices\[.*\].Assertion.(Members|CardinalityValidators)") ||
            Regex.IsMatch(memberInfo.Path, @".*.Definition.Type\[.*");

        [Fact]
        public async T.Task TestOpenValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEOPEN, "Patient.identifier");

            // This is a *open* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceValidator(false, false, ResultAssertion.SUCCESS, _fixedSlice,
                getPatternSlice(TestProfileArtifactSource.VALUESLICETESTCASEOPEN));

            var st = slice.ToJson().ToString();
            var et = expectedSlice.ToJson().ToString();

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx))
                .UsingCanonicalCompare());
        }


        [Fact]
        public async T.Task TestDefaultSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEWITHDEFAULT, "Patient.identifier");

            var expectedSlice = new SliceValidator(false, false, new ElementSchema(
#if STU3
                // STU3 handles ElementId in another way during Snapshotting
                "#Patient.identifier:PatternBinding"
#else
                "#Patient.identifier:@default"
#endif
                ), _fixedSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => Regex.IsMatch(ctx.Path, @"Default.(Members|CardinalityValidators)") || excludeSliceAssertionCheck(ctx))
                .UsingCanonicalCompare()
                );

            // Also make sure the default slice has a child "system" that has a binding to demobinding.
            ((ElementSchema)((ElementSchema)slice.Default).Members.OfType<ChildrenValidator>().Single().ChildList["system"])
                .Members.OfType<BindingValidator>().Single().ValueSetUri
                .Should().Be((Canonical)"http://example.com/demobinding");
        }

        [Fact]
        public async T.Task TestDiscriminatorlessGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.DISCRIMINATORLESS, "Patient.identifier");

            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("Fixed", condition: new ElementSchema("#Patient.identifier:Fixed:condition"), assertion: ResultAssertion.SUCCESS),
                    new SliceValidator.SliceCase("PatternBinding", new ElementSchema("#Patient.identifier:PatternBinding:condition"), assertion: ResultAssertion.SUCCESS));

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => Regex.IsMatch(ctx.Path, @"Slices\[.*\].Condition.(Members|CardinalityValidators)"))
                .UsingCanonicalCompare());
        }

        [Fact]
        public async T.Task TestTypeAndProfileSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.TYPEANDPROFILESLICE, "Questionnaire.item.enableWhen");

            // Note that we have multiple disciminators, this is visible in slice 1. In slice 2, they have
            // been optimized away, since the profile discriminator no profiles specified on the typeRef element.
            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("string", condition: new AllValidator(shortcircuitEvaluation: true,
                        new PathSelectorValidator("question", new SchemaReferenceValidator(TestProfileArtifactSource.PROFILEDSTRING)),
                        new PathSelectorValidator("answer", new FhirTypeLabelValidator("string"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:string")),
                    new SliceValidator.SliceCase("boolean", condition: new PathSelectorValidator("answer", new FhirTypeLabelValidator("boolean")),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:boolean")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx))
                    .UsingCanonicalCompare());
        }

        [Fact]
        public async T.Task TestReferencedTypeAndProfileSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.REFERENCEDTYPEANDPROFILESLICE, "Questionnaire.item.enableWhen");

            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("Only1Slice", condition: new AllValidator(shortcircuitEvaluation: true,
                        new PathSelectorValidator("answer.resolve()", new SchemaReferenceValidator(TestProfileArtifactSource.PATTERNSLICETESTCASE)),
                        new PathSelectorValidator("answer.resolve()", new FhirTypeLabelValidator("Patient"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:Only1Slice")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx))
                    .UsingCanonicalCompare());
        }

        [Fact]
        public async T.Task TestExistSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.EXISTSLICETESTCASE, "Patient.name");

            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                new SliceValidator.SliceCase("Exists",
                    condition: new PathSelectorValidator("family", new CardinalityValidator(1, 1)),
                    assertion: new ElementSchema("#Patient.name:Exists")),
                new SliceValidator.SliceCase("NotExists",
                    condition: new PathSelectorValidator("family", new CardinalityValidator(0, 0)),
                    assertion: new ElementSchema("#Patient.name:NotExists"))
                );

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx))
                .UsingCanonicalCompare()
                );
        }


        [Fact(Skip = "Due to perf optimizations, we're only checking the cardinality of the root slice when there are no elements. Could be fixed by creating a correct root CardinalityValidator at compile time, but too much work for this old corner case.")]
        public async T.Task IntroAndSliceShouldValidateCardinalityIndependently()
        {
            // See https://chat.fhir.org/#narrow/stream/179177-conformance/topic/Extension.20element.20cardinality
            var assertions = await createElement(TestProfileArtifactSource.INCOMPATIBLECARDINALITYTESTCASE, "Patient.identifier");

            // The cardinality of the intro (originally set to 0..1), should have been updated to be at least the sum of the minimums of the slices (1+1 = 2)                            
            assertions.Should().ContainSingle().Which.Should().BeEquivalentTo(new CardinalityValidator(2, 2));
        }

        [Fact]
        public async T.Task TestResliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.RESLICETESTCASE, "Patient.telecom");

#if STU3
            var contactPointSystem = "http://hl7.org/fhir/ValueSet/contact-point-system";
            var contactPointUse = "http://hl7.org/fhir/ValueSet/contact-point-use";
#else
            var contactPointSystem = $"http://hl7.org/fhir/ValueSet/contact-point-system|{ModelInfo.Version}";
            var contactPointUse = $"http://hl7.org/fhir/ValueSet/contact-point-use|{ModelInfo.Version}";
#endif
            var expectedSlice = new SliceValidator(false, true, ResultAssertion.SUCCESS,
            new SliceValidator.SliceCase("phone", new PathSelectorValidator("system", new AllValidator(shortcircuitEvaluation: true,
                    new FixedValidator(new Code("phone")),
                    new BindingValidator(contactPointSystem, BindingValidator.BindingStrength.Required, context: "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase#Patient.telecom.system"))),
                        new ElementSchema("#Patient.telecom:phone")),
                new SliceValidator.SliceCase("email", new PathSelectorValidator("system", new AllValidator(shortcircuitEvaluation: true,
                    new FixedValidator(new Code("email")),
                    new BindingValidator(contactPointSystem, BindingValidator.BindingStrength.Required, context: "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase#Patient.telecom.system"))),
                        new ElementSchema("#Patient.telecom:email"))
                );

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));

            testResliceInSlice2(slice.Slices[1]);

            void testResliceInSlice2(SliceValidator.SliceCase slice2)
            {
                var es = (ElementSchema)slice2.Assertion;
                es.Members.OfType<SliceValidator>().Should().ContainSingle();
                var subslice = es.Members.OfType<SliceValidator>().Single();

                var email = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("email/home", new PathSelectorValidator("use", new AllValidator(shortcircuitEvaluation: true,
                        new FixedValidator(new Code("home")),
                        new BindingValidator(contactPointUse, BindingValidator.BindingStrength.Required, context: "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase#Patient.telecom.use"))),
                            new ElementSchema("#Patient.telecom:email/home")),
                    new SliceValidator.SliceCase("email/work", new PathSelectorValidator("use", new AllValidator(shortcircuitEvaluation: true,
                        new FixedValidator(new Code("work")),
                        new BindingValidator(contactPointUse, BindingValidator.BindingStrength.Required, context: "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase#Patient.telecom.use"))),
                            new ElementSchema("#Patient.telecom:email/work"))
                    );

                subslice.Should().BeEquivalentTo(email, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
            }
        }
    }
}

