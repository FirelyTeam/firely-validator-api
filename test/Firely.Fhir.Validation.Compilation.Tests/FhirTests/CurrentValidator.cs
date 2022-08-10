using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;
using System.Diagnostics;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class CurrentValidator : ITestValidator
    {
        private static readonly IResourceResolver BASE_RESOLVER = new CachedResolver(ZipSource.CreateValidationSource());

        private readonly string[] _unsupportedTests = new[]
        { 
            // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
            "cda/example", "cda/example-no-styles",
        };

        private readonly Stopwatch _stopWatch;

        public static ITestValidator Create() => new CurrentValidator();

        public string Name => "Current";

        public string[] UnvalidatableTests => _unsupportedTests;

        public CurrentValidator(Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
        }

        public ExpectedResult? GetExpectedResults(IValidatorEnginesResults engine) => engine.FirelySDKCurrent;
        public void SetExpectedResults(IValidatorEnginesResults engine, ExpectedResult result) => engine.FirelySDKCurrent = result;

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IResourceResolver? resolver, string? profile = null)
        {
            var extendeResolver = resolver is null ? BASE_RESOLVER : new SnapshotSource(new MultiResolver(BASE_RESOLVER, resolver));

            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = extendeResolver,
                TerminologyService = new LocalTerminologyService(extendeResolver.AsAsync()),
            };

            var validator = new Hl7.Fhir.Validation.Validator(settings);

            _stopWatch.Start();
            var outcome = profile is null ? validator.Validate(instance) : validator.Validate(instance, profile);
            _stopWatch.Stop();
            return outcome.RemoveDuplicateMessages();
        }

        public bool CannotValidateTest(TestCase c) => UnvalidatableTests.Contains(c.Name) && ModelInfo.CheckMinorVersionCompatibility(c.Version ?? "5.0");
    }
}
