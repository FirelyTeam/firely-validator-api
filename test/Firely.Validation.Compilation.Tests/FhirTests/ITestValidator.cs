using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal interface ITestValidator
    {
        OperationOutcome Validate(ITypedElement instance, IResourceResolver resolver, string? profile = null);

        ExpectedResult? GetExpectedResults(IValidatorEngines engine);

        void SetExpectedResults(IValidatorEngines engine, ExpectedResult result);
        bool ShouldIgnoreTest(string name);
    }
}
