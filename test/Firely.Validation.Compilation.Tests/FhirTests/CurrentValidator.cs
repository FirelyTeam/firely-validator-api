using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;
using System.Collections.Generic;
using System.Diagnostics;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class CurrentValidator : ITestValidator
    {
        private readonly List<string> _ignoreTestList = new();
        private readonly IResourceResolver? _resourceResolver;
        private Stopwatch _stopWatch;

        public static ITestValidator INSTANCE = new CurrentValidator();

        public CurrentValidator(IResourceResolver? resolver = null, Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
            _resourceResolver = resolver;
        }

        public ExpectedResult? GetExpectedResults(IValidatorEnginesResults engine) => engine.FirelySDKCurrent;
        public void SetExpectedResults(IValidatorEnginesResults engine, ExpectedResult result) => engine.FirelySDKCurrent = result;
        public bool ShouldIgnoreTest(string name) => _ignoreTestList.Contains(name);

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
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

            var validator = new Validator(settings);

            _stopWatch.Start();
            var outcome = profile is null ? validator.Validate(instance) : validator.Validate(instance, profile);
            _stopWatch.Stop();
            return outcome;
        }

    }
}
