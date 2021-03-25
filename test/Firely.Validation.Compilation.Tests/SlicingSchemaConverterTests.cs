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

        private async T.Task<SliceAssertion> createSliceForElement(string canonical, string childPath)
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(canonical)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.JumpToFirst(childPath));
            return _fixture.Converter.CreateSliceAssertion(sdNav);
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
            var effe = slice.ToJson().ToString();

            // This is a *closed* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceAssertion(false, false, SliceClosedAssertion, FixedSlice, PatternSlice);

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeSliceAssertionCheck(ctx)));

            /*
             * This was the original unit test, but I have replaced it with the BeEquivalentTo above.
             * Just keeping it here, in case I find something I cannot test with BeEquivalentTo
             * 
            slice.Should().NotBeNull();
            slice!.Ordered.Should().BeFalse();
            assertIsClosed(slice);
            slice.Slices.Should().HaveCount(2);

            testSlice1(slice.Slices[0]);
            // testSlice2(slice.Slices[1]);
            static void testSlice1(SliceAssertion.Slice slice1)
            {
                slice1.Name.Should().Be("Fixed");
                slice1.Condition.Should().BeOfType<PathSelectorAssertion>();
                var pa = (PathSelectorAssertion)slice1.Condition;
                pa.Path.Should().Be("system");
                pa.Other.Should().BeOfType<Fixed>();
                var fix = (Fixed)pa.Other;
                fix.FixedValue.Should().BeEquivalentTo(new FhirUri("http://example.com/some-bsn-uri").ToTypedElement());

                slice1.Assertion.Should().BeOfType<ElementSchema>();
                var es = (ElementSchema)slice1.Assertion;
                es.Id.Should().Be("#Patient.identifier:Fixed");
                //assertion should be schema with id of element.id or path + slicename
            }

            internal static void assertIsClosed(SliceAssertion sa)
            {
                sa.Default.Should().NotBeNull(because: "closed slices should have a default assertion");
                sa.Default.Should().BeOfType<ResultAssertion>();
                var ra = (ResultAssertion)sa.Default;
                ra.Result.Should().Be(ValidationResult.Failure);
                ra.Evidence.Should().HaveCount(1);
                ra.Evidence.Single().Should().BeOfType<IssueAssertion>();
                var ia = (IssueAssertion)ra.Evidence.Single();
                ia.IssueNumber.Should().Be(1026);
            }*/
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
            var effe = slice.ToJson().ToString();

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
        public async T.Task TestResliceGeneration()
        {
            var slice = await createSliceForElement(TestProfileArtifactSource.RESLICETESTCASE, "Patient.telecom");
            var effe = slice.ToJson().ToString();

        }
    }
}

