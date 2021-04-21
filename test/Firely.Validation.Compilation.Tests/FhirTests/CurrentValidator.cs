using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class CurrentValidator : ITestValidator
    {
        private readonly List<string> _ignoreTestList = new();
        public static ITestValidator INSTANCE = new CurrentValidator();

        public CurrentValidator()
        {

        }

        public ExpectedResult? GetExpectedResults(IValidatorEngines engine) => engine.FirelySDKCurrent;
        public void SetExpectedResults(IValidatorEngines engine, ExpectedResult result) => engine.FirelySDKCurrent = result;
        public bool ShouldIgnoreTest(string name) => _ignoreTestList.Contains(name);

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public OperationOutcome Validate(ITypedElement instance, IResourceResolver resolver, string? profile = null)
        {
            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = resolver,
                TerminologyService = new LocalTerminologyService(resolver.AsAsync()),
            };

            var validator = new Validator(settings);

            return profile is null ? validator.Validate(instance) : validator.Validate(instance, profile);
        }

    }
}
