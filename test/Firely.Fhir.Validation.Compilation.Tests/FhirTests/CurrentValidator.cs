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
        private readonly string[] _unsupportedTests = new[]
        { 
            // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
            "cda/example", "cda/example-no-styles",

            // these tests will cause a circular validation and thus a stack overflow.
            "message", "message-empty-entry",

            // do not run an Empty testcase
            ValidationManifestDataSourceAttribute.EMPTY_TESTCASE_NAME
        };

        private readonly IResourceResolver? _resourceResolver;
        private readonly Stopwatch _stopWatch;

        public static ITestValidator INSTANCE = new CurrentValidator();

        public string Name => "Current";

        public string[] UnvalidatableTests => _unsupportedTests;

        public CurrentValidator(IResourceResolver? resolver = null, Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
            _resourceResolver = resolver;
        }

        public ExpectedResult? GetExpectedResults(IValidatorEnginesResults engine) => engine.FirelySDKCurrent;
        public void SetExpectedResults(IValidatorEnginesResults engine, ExpectedResult result) => engine.FirelySDKCurrent = result;

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IResourceResolver resolver, string? profile = null)
        {
            // resolver of class has priority over the incoming resolver from this function
            var resResolver = _resourceResolver ?? resolver;

            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = resResolver,
                TerminologyService = new LocalTerminologyService(resResolver.AsAsync()),
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
