using Firely.Fhir.Validation.Compilation;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Xunit;

namespace Hl7.Fhir.Validation.Tests
{
    public class ValidationFixture
    {
        public IResourceResolver Resolver => (CachedResolver)AsyncResolver; // works, this is a CachedResolver

        public IAsyncResourceResolver AsyncResolver { get; }

        public ValidationSettings Settings { get; }

        public ValidationFixture()
        {
            AsyncResolver = new CachedResolver(
                    new StructureDefinitionCorrectionsResolver(
                    new MultiResolver(
                        new BasicValidationTests.BundleExampleResolver("TestData"),
                        new DirectorySource("TestData"),
                        new TestProfileArtifactSource(),
                        ZipSource.CreateValidationSource())));

            Settings = new ValidationSettings()
            {
                ResourceResolver = Resolver,
                GenerateSnapshot = true,
                EnableXsdValidation = true,
                Trace = false,
                ResolveExternalReferences = true
            };
        }

        public Validator GetNew() => new(Settings);
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
        public void UnresolvableExtensionAreJustWarnings()
        {
            var validator = Fixture.GetNew();

            var p = new Patient
            {
                Active = true
            };

            p.AddExtension("http://nu.nl", new FhirBoolean(false), isModifier: false);
            var result = validator.Validate(p);
            result.Warnings.Should().Be(1);
            result.Errors.Should().Be(0);


            p.AddExtension("http://nu.nl/modifier", new FhirBoolean(false), isModifier: true);
            result = validator.Validate(p);
            result.Warnings.Should().Be(1);
            result.Errors.Should().Be(1);

            var newP = new Patient
            {
                Active = true,
                Meta = new()
            };
            newP.Meta.ProfileElement.Add(new FhirUri("http://example.org/unresolvable"));
            result = validator.Validate(newP);
            result.Warnings.Should().Be(0);
            result.Errors.Should().Be(1);
        }

    }

}
