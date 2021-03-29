using Firely.Fhir.Validation;
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

namespace Firely.Validation.Compilation.Tests
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

        private async T.Task<SliceAssertion> createSliceForElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            return (SliceAssertion)_fixture.Converter.CreateSliceAssertion(sdNav);
        }

        private readonly ResultAssertion SliceClosedAssertion = new ResultAssertion(ValidationResult.Failure,
                  new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE,
                      "TODO: location?", "Element does not match any slice and the group is closed."));

        private readonly SliceAssertion.Slice FixedSlice = new SliceAssertion.Slice("Fixed",
                    new PathSelectorAssertion("system", new Fixed(new FhirUri("http://example.com/some-bsn-uri").ToTypedElement())),
                    new ElementSchema("#Patient.identifier:Fixed"));

        private readonly SliceAssertion.Slice PatternSlice = new SliceAssertion.Slice("PatternBinding",
                    new PathSelectorAssertion("system", new AllAssertion(
                        new Pattern(new FhirUri("http://example.com/someuri").ToTypedElement()),
                        new BindingAssertion("http://example.com/demobinding", strength: BindingAssertion.BindingStrength.Required))),
                    new ElementSchema("#Patient.identifier:PatternBinding"));

        [Fact]
        public async T.Task TestValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASE, "Patient.identifier");

            // This is a *closed* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion, FixedSlice, PatternSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        static bool excludeSliceAssertionCheck(IMemberInfo memberInfo) =>
            Regex.IsMatch(memberInfo.SelectedMemberPath, @"Slices\[.*\].Assertion.Members");

        [Fact]
        public async T.Task TestOpenValueSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEOPEN, "Patient.identifier");

            // This is a *open* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceAssertion(false, false, ResultAssertion.SUCCESS, FixedSlice, PatternSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }


        [Fact]
        public async T.Task TestDefaultSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.VALUESLICETESTCASEWITHDEFAULT, "Patient.identifier");

            var expectedSlice = new SliceAssertion(false, false,
                new ElementSchema("#Patient.identifier:@default"), FixedSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => Regex.IsMatch(ctx.SelectedMemberPath, @"Default.Members") || excludeSliceAssertionCheck(ctx)));

            // Also make sure the default slice has a child "system" that has a binding to demobinding.
            ((ElementSchema)((ElementSchema)slice.Default).Members.OfType<Children>().Single().ChildList["system"])
                .Members.OfType<BindingAssertion>().Single().ValueSetUri.Should().Be("http://example.com/demobinding");
        }

        [Fact]
        public async T.Task TestDiscriminatorlessGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.DISCRIMINATORLESS, "Patient.identifier");

            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion,
                    new SliceAssertion.Slice("Fixed", condition: new ElementSchema("#Patient.identifier:Fixed"), assertion: ResultAssertion.SUCCESS),
                    new SliceAssertion.Slice("PatternBinding", new ElementSchema("#Patient.identifier:PatternBinding"), assertion: ResultAssertion.SUCCESS));

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
            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion,
                    new SliceAssertion.Slice("string", condition: new AllAssertion(
                        new PathSelectorAssertion("question", new ReferenceAssertion("http://example.com/profile1")),
                        new PathSelectorAssertion("answer", new FhirTypeLabel("string"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:string")),
                    new SliceAssertion.Slice("boolean", condition: new PathSelectorAssertion("answer", new FhirTypeLabel("boolean")),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:boolean")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        [Fact]
        public async T.Task TestReferencedTypeAndProfileSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.REFERENCEDTYPEANDPROFILESLICE, "Questionnaire.item.enableWhen");

            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion,
                    new SliceAssertion.Slice("Only1Slice", condition: new AllAssertion(
                        new PathSelectorAssertion("answer.resolve()", new ReferenceAssertion(TestProfileArtifactSource.PATTERNSLICETESTCASE)),
                        new PathSelectorAssertion("answer.resolve()", new FhirTypeLabel("Patient"))),
                        assertion: new ElementSchema("#Questionnaire.item.enableWhen:Only1Slice")));

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
        }

        [Fact]
        public async T.Task TestExistSliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.EXISTSLICETESTCASE, "Patient.name");

            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion,
                new SliceAssertion.Slice("Exists",
                    condition: new PathSelectorAssertion("family", new CardinalityAssertion(1, "1")),
                    assertion: new ElementSchema("#Patient.name:Exists")),
                new SliceAssertion.Slice("NotExists",
                    condition: new PathSelectorAssertion("family", new CardinalityAssertion(0, "0")),
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

            var cardinalityOfIntro = elementSchema.Members.OfType<CardinalityAssertion>().SingleOrDefault();
            cardinalityOfIntro.Should().BeEquivalentTo(new CardinalityAssertion(0, 1));

            // there should be a *sibling* slice that will check the cardinalities of each slice
            // as well. This means both the cardinality constraint for the element (coming from the
            // slice intro, above) AND the cardinality for each slice are run.
            var slicing = elementSchema.Members.OfType<SliceAssertion>().FirstOrDefault();
            slicing.Should().NotBeNull();
            var slice0Schema = slicing.Slices[0].Assertion as ElementSchema;
            Assert.NotNull(slice0Schema);
            var slice0Cardinality = slice0Schema!.Members.OfType<CardinalityAssertion>().SingleOrDefault();
            slice0Cardinality.Should().BeEquivalentTo(new CardinalityAssertion(1, "1"));

            // just to make sure, a bit of an integration tests with an actualy instance.
            var noIdentifiers = Enumerable.Empty<ITypedElement>();
            var result = await elementSchema.Validate(noIdentifiers, _fixture.NewValidationContext());

            // this should report the instance count is not within the cardinality range of 1..1 of the slice
            result.GetIssueAssertions().Should().Contain(ia => ia.IssueNumber == 1028 && ia.Message.Contains("1..1"));

            var twoIdentifiers = new[] { new Identifier("sys", "val"), new Identifier("sys2", "val2") };
            result = await elementSchema.Validate(twoIdentifiers.Select(i => i.ToTypedElement()), _fixture.NewValidationContext());

            // this should report the instance count is not within the cardinality range of 0..1 of the intro
            result.GetIssueAssertions().Should().Contain(ia => ia.IssueNumber == 1028 && ia.Message.Contains("0..1"));
        }

        [Fact]
        public async T.Task TestResliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.RESLICETESTCASE, "Patient.telecom");

            var expectedSlice = new SliceAssertion(false, true, ResultAssertion.SUCCESS,
                new SliceAssertion.Slice("phone", new PathSelectorAssertion("system", new AllAssertion(
                    new Fixed(new Code("phone").ToTypedElement()),
                    new BindingAssertion("http://hl7.org/fhir/ValueSet/contact-point-system|4.0.1", BindingAssertion.BindingStrength.Required,
                            description: "Telecommunications form for contact point."))),
                        new ElementSchema("#Patient.telecom:phone")),
                new SliceAssertion.Slice("email", new PathSelectorAssertion("system", new AllAssertion(
                    new Fixed(new Code("email").ToTypedElement()),
                    new BindingAssertion("http://hl7.org/fhir/ValueSet/contact-point-system|4.0.1", BindingAssertion.BindingStrength.Required,
                            description: "Telecommunications form for contact point."))),
                        new ElementSchema("#Patient.telecom:email"))
                );

            slice.Should().BeEquivalentTo(expectedSlice, options => options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));

            testResliceInSlice2(slice.Slices[1]);

            void testResliceInSlice2(SliceAssertion.Slice slice2)
            {
                var es = (ElementSchema)slice2.Assertion;
                es.Members.OfType<SliceAssertion>().Should().ContainSingle();
                var subslice = es.Members.OfType<SliceAssertion>().Single();

                var email = new SliceAssertion(false, false, SliceClosedAssertion,
                    new SliceAssertion.Slice("email/home", new PathSelectorAssertion("use", new AllAssertion(
                        new Fixed(new Code("home").ToTypedElement()),
                        new BindingAssertion("http://hl7.org/fhir/ValueSet/contact-point-use|4.0.1", BindingAssertion.BindingStrength.Required,
                                description: "Use of contact point."))),
                            new ElementSchema("#Patient.telecom:email/home")),
                    new SliceAssertion.Slice("email/work", new PathSelectorAssertion("use", new AllAssertion(
                        new Fixed(new Code("work").ToTypedElement()),
                        new BindingAssertion("http://hl7.org/fhir/ValueSet/contact-point-use|4.0.1", BindingAssertion.BindingStrength.Required,
                                description: "Use of contact point."))),
                            new ElementSchema("#Patient.telecom:email/work"))
                    );

                subslice.Should().BeEquivalentTo(email, options => options.IncludingAllRuntimeProperties()
                    .Excluding(ctx => excludeSliceAssertionCheck(ctx)));
            }
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("A", null, true)]
        [InlineData("A/B", null, false)]
        [InlineData("A", "A", false)]
        [InlineData("B", "A", false)]
        [InlineData("A/B", "A", true)]
        [InlineData("A/B/C", "A", false)]
        [InlineData("B/C", "A", false)]
        [InlineData("AA", "A", false)]
        [InlineData("A/BB", "A/B", false)]
        public void DetectsResliceCorrectly(string child, string parent, bool result)
        {
            Assert.Equal(result, Firely.Validation.Compilation.ElementDefinitionNavigatorExtensions.IsResliceOf(child, parent));
        }
    }
}

