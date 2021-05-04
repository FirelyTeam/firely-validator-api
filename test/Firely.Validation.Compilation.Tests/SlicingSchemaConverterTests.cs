/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using FluentAssertions.Equivalency;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation.Tests
{

    public class SlicingSchemaConverterTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public SlicingSchemaConverterTests(SchemaConverterFixture fixture) => _fixture = fixture;

        private async T.Task<ElementSchema> createElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            return _fixture.Converter.ConvertElement(sdNav);
        }

        private async T.Task<SliceValidator> createSliceForElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            return (SliceValidator)_fixture.Converter.CreateSliceAssertion(sdNav);
        }

        private readonly ResultAssertion _sliceClosedAssertion = new(ValidationResult.Failure,
                  new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE,
                      "TODO: location?", "Element does not match any slice and the group is closed."));

        private readonly SliceValidator.SliceCase _fixedSlice = new("Fixed",
                    new PathSelectorValidator("system", new FixedValidator(new FhirUri("http://example.com/some-bsn-uri").ToTypedElement())),
                    new ElementSchema("#Patient.identifier:Fixed"));

        private readonly SliceValidator.SliceCase _patternSlice = new("PatternBinding",
                    new PathSelectorValidator("system", new AllValidator(
                        new PatternValidator(new FhirUri("http://example.com/someuri").ToTypedElement()),
                        new BindingValidator("http://example.com/demobinding", strength: BindingValidator.BindingStrength.Required))),
                    new ElementSchema("#Patient.identifier:PatternBinding"));

        [Fact]
        public async T.Task TestValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASE, "Patient.identifier");

            // This is a *closed* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion, _fixedSlice, _patternSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        private static bool excludeSliceAssertionCheck(IMemberInfo memberInfo) =>
            Regex.IsMatch(memberInfo.SelectedMemberPath, @"Slices\[.*\].Assertion.Members");

        [Fact]
        public async T.Task TestOpenValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEOPEN, "Patient.identifier");

            // This is a *open* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceValidator(false, false, ResultAssertion.SUCCESS, _fixedSlice, _patternSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }


        [Fact]
        public async T.Task TestDefaultSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEWITHDEFAULT, "Patient.identifier");

            var expectedSlice = new SliceValidator(false, false,
                new ElementSchema("#Patient.identifier:@default"), _fixedSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => Regex.IsMatch(ctx.SelectedMemberPath, @"Default.Members") || excludeSliceAssertionCheck(ctx)));

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
                    new SliceValidator.SliceCase("Fixed", condition: new ElementSchema("#Patient.identifier:Fixed"), assertion: ResultAssertion.SUCCESS),
                    new SliceValidator.SliceCase("PatternBinding", new ElementSchema("#Patient.identifier:PatternBinding"), assertion: ResultAssertion.SUCCESS));

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => Regex.IsMatch(ctx.SelectedMemberPath, @"Slices\[.*\].Condition.Members")));
        }

        [Fact]
        public async T.Task TestTypeAndProfileSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.TYPEANDPROFILESLICE, "Questionnaire.item.enableWhen");

            // Note that we have multiple disciminators, this is visible in slice 1. In slice 2, they have
            // been optimized away, since the profile discriminator no profiles specified on the typeRef element.
            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("string", condition: new AllValidator(
                        new PathSelectorValidator("question", new SchemaReferenceValidator("http://example.com/profile1")),
                        new PathSelectorValidator("answer", new FhirTypeLabelValidator("string"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:string")),
                    new SliceValidator.SliceCase("boolean", condition: new PathSelectorValidator("answer", new FhirTypeLabelValidator("boolean")),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:boolean")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        [Fact]
        public async T.Task TestReferencedTypeAndProfileSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.REFERENCEDTYPEANDPROFILESLICE, "Questionnaire.item.enableWhen");

            var expectedSlice = new SliceValidator(false, false, _sliceClosedAssertion,
                    new SliceValidator.SliceCase("Only1Slice", condition: new AllValidator(
                        new PathSelectorValidator("answer.resolve()", new SchemaReferenceValidator(TestProfileArtifactSource.PATTERNSLICETESTCASE)),
                        new PathSelectorValidator("answer.resolve()", new FhirTypeLabelValidator("Patient"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:Only1Slice")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
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
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        [Fact]
        public async T.Task IntroAndSliceShouldValidateCardinalityIndependently()
        {
            // See https://chat.fhir.org/#narrow/stream/179177-conformance/topic/Extension.20element.20cardinality
            var elementSchema = await createElement(TestProfileArtifactSource.INCOMPATIBLECARDINALITYTESTCASE, "Patient.identifier");
            var effe = elementSchema.ToJson().ToString();

            var cardinalityOfIntro = elementSchema.Members.OfType<CardinalityValidator>().SingleOrDefault();
            cardinalityOfIntro.Should().BeEquivalentTo(new CardinalityValidator(0, 1));

            // there should be a *sibling* slice that will check the cardinalities of each slice
            // as well. This means both the cardinality constraint for the element (coming from the
            // slice intro, above) AND the cardinality for each slice are run.
            var slicing = elementSchema.Members.OfType<SliceValidator>().FirstOrDefault();
            slicing.Should().NotBeNull();
            var slice0Schema = slicing.Slices[0].Assertion as ElementSchema;
            Assert.NotNull(slice0Schema);
            var slice0Cardinality = slice0Schema!.Members.OfType<CardinalityValidator>().SingleOrDefault();
            slice0Cardinality.Should().BeEquivalentTo(new CardinalityValidator(1, 1));

            // just to make sure, a bit of an integration tests with an actualy instance.
            var noIdentifiers = Enumerable.Empty<ITypedElement>();
            var result = await elementSchema.Validate(noIdentifiers, "test location", _fixture.NewValidationContext());

            // this should report the instance count is not within the cardinality range of 1..1 of the slice
            result.Evidence.OfType<IssueAssertion>().Should().Contain(ia => ia.IssueNumber == 1028 && ia.Message.Contains("1..1"));

            var twoIdentifiers = new[] { new Identifier("sys", "val"), new Identifier("sys2", "val2") };
            result = await elementSchema.Validate(twoIdentifiers.Select(i => i.ToTypedElement()), "test location", _fixture.NewValidationContext());

            // this should report the instance count is not within the cardinality range of 0..1 of the intro
            result.Evidence.OfType<IssueAssertion>().Should().Contain(ia => ia.IssueNumber == 1028 && ia.Message.Contains("0..1"));
        }

        [Fact]
        public async T.Task TestResliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.RESLICETESTCASE, "Patient.telecom");

            var expectedSlice = new SliceValidator(false, true, ResultAssertion.SUCCESS,
                new SliceValidator.SliceCase("phone", new PathSelectorValidator("system", new AllValidator(
                    new FixedValidator(new Code("phone").ToTypedElement()),
                    new BindingValidator("http://hl7.org/fhir/ValueSet/contact-point-system|4.0.1", BindingValidator.BindingStrength.Required))),
                        new ElementSchema("#Patient.telecom:phone")),
                new SliceValidator.SliceCase("email", new PathSelectorValidator("system", new AllValidator(
                    new FixedValidator(new Code("email").ToTypedElement()),
                    new BindingValidator("http://hl7.org/fhir/ValueSet/contact-point-system|4.0.1", BindingValidator.BindingStrength.Required))),
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
                    new SliceValidator.SliceCase("email/home", new PathSelectorValidator("use", new AllValidator(
                        new FixedValidator(new Code("home").ToTypedElement()),
                        new BindingValidator("http://hl7.org/fhir/ValueSet/contact-point-use|4.0.1", BindingValidator.BindingStrength.Required))),
                            new ElementSchema("#Patient.telecom:email/home")),
                    new SliceValidator.SliceCase("email/work", new PathSelectorValidator("use", new AllValidator(
                        new FixedValidator(new Code("work").ToTypedElement()),
                        new BindingValidator("http://hl7.org/fhir/ValueSet/contact-point-use|4.0.1", BindingValidator.BindingStrength.Required))),
                            new ElementSchema("#Patient.telecom:email/work"))
                    );

                subslice.Should().BeEquivalentTo(email, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
            }
        }
    }
}

