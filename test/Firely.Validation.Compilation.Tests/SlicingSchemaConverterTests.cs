using Firely.Fhir.Validation;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System.Text.RegularExpressions;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Validation.Compilation.Tests
{

    public class SlicingSchemaConverterTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public SlicingSchemaConverterTests(SchemaConverterFixture fixture) => _fixture = fixture;

        [Fact]
        public async T.Task TestValueSliceGeneration()
        {
            var sd = (await _fixture.ResourceResolver.ResolveByCanonicalUriAsync(TestProfileArtifactSource.VALUESLICETESTCASE)) as StructureDefinition;
            var sdNav = ElementDefinitionNavigator.ForSnapshot(sd);
            sdNav.MoveToFirstChild();
            Assert.True(sdNav.MoveToChild("identifier"));
            var converter = new SchemaConverter(_fixture.ResourceResolver);
            var slice = converter.CreateSliceAssertion(sdNav) as SliceAssertion;

            // This is a *closed* slice, with a value/pattern discriminator.
            // The first slice has a fixed constraint, the second slice has both a pattern and a binding constraint.
            var expectedSlice = new SliceAssertion(false,
                new ResultAssertion(ValidationResult.Failure,
                  new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed.")),
                 new SliceAssertion.Slice("Fixed",
                    new PathSelectorAssertion("system", new Fixed(new FhirUri("http://example.com/some-bsn-uri").ToTypedElement())),
                    new ElementSchema("#Patient.identifier:Fixed")),
                 new SliceAssertion.Slice("PatternBinding",
                    new PathSelectorAssertion("system", new AllAssertion(
                        new Pattern(new FhirUri("http://example.com/someuri").ToTypedElement()),
                        new BindingAssertion("http://example.com/demobinding", strength: BindingAssertion.BindingStrength.Required)
                        )),
                    new ElementSchema("#Patient.identifier:PatternBinding"))
                    );

            slice.Should().BeEquivalentTo(expectedSlice, options =>
                options.IncludingAllRuntimeProperties()
                .Excluding(ctx => excludeMember(ctx)));

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

        static bool excludeMember(IMemberInfo memberInfo) =>
            Regex.IsMatch(memberInfo.SelectedMemberPath, @"Slices\[.*\].Assertion.Members");


        public async T.Task TestDefaultSliceGeneration()
        {

        }

        public async T.Task TestMultipleDiscriminatorsGeneration()
        {
            // also test open slice (different default)
        }

        public async T.Task TestDiscriminatorlessGeneration()
        {

        }

        public async T.Task TestResliceGeneration()
        {

        }

    }
}

