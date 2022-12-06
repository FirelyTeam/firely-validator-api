using Firely.Fhir.Validation.Compilation;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using System.IO;
using System.Linq;
using Xunit;

namespace Hl7.Fhir.Validation.Tests
{
    public class ValidationFixture
    {
        public IResourceResolver Resolver => (CachedResolver)AsyncResolver; // works, this is a CachedResolver

        public IAsyncResourceResolver AsyncResolver { get; }

        public Validator Validator { get; }
        public ValidationFixture()
        {
            AsyncResolver = new CachedResolver(
                    new StructureDefinitionCorrectionsResolver(
                    new MultiResolver(
                        new BasicValidationTests.BundleExampleResolver("TestData"),
                        new DirectorySource("TestData"),
                        new TestProfileArtifactSource(),
                        ZipSource.CreateValidationSource())));

            var ctx = new ValidationSettings()
            {
                ResourceResolver = Resolver,
                GenerateSnapshot = true,
                EnableXsdValidation = true,
                Trace = false,
                ResolveExternalReferences = true
            };


            Validator = new Validator(ctx);
        }
    }

    [Trait("Category", "Validation")]
    public class ProfileAssertionTests : IClassFixture<ValidationFixture>
    {
        public ValidationFixture Fixture { get; }

        public ProfileAssertionTests(ValidationFixture fixture)
        {
            Fixture = fixture;
        }


        [Fact]
        public void TestIssue2105()
        {
            var json = File.ReadAllText(Path.Combine("TestData", "validation", "Delana41_Dibbert-first-part.json"));
            var bundle = new FhirJsonParser().Parse<Bundle>(json);
            var outcome = Fixture.Validator.Validate(bundle);

            // The last two issues are details and should have a hierarchy +1 compared to the parent issue (its parent)
            var rest = outcome.Issue.SkipWhile(i => i.Details.Coding.First().Code != Issue.PROCESSING_PROGRESS.Code.ToString());
            var parentHierarchy = rest.First().HierarchyLevel;
            Assert.True(rest.Skip(1).All(c => c.HierarchyLevel == parentHierarchy + 1));
        }

        [Fact]
        public void UnresolvableExtensionAreJustWarnings()
        {
            var p = new Patient
            {
                Active = true
            };

            p.AddExtension("http://nu.nl", new FhirBoolean(false), isModifier: false);
            var result = Fixture.Validator.Validate(p);
            result.Warnings.Should().Be(1);
            result.Errors.Should().Be(0);


            p.AddExtension("http://nu.nl/modifier", new FhirBoolean(false), isModifier: true);
            result = Fixture.Validator.Validate(p);
            result.Warnings.Should().Be(1);
            result.Errors.Should().Be(1);

            var newP = new Patient
            {
                Active = true,
                Meta = new()
            };
            newP.Meta.ProfileElement.Add(new FhirUri("http://example.org/unresolvable"));
            result = Fixture.Validator.Validate(newP);
            result.Warnings.Should().Be(0);
            result.Errors.Should().Be(1);
        }

    }

}
