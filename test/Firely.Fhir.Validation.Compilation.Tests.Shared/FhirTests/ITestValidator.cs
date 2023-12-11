using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal interface ITestValidator
    {
        /// <summary>
        /// Human-readable name for the validator
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The list of tests names that must not be presented to the validator, since
        /// it cannot handle them. Note: this is not about incorrect results, but the
        /// ability to run them at all.
        /// </summary>
        string[] UnvalidatableTests { get; }

        /// <summary>
        /// Indicates whether this validator is able to run the given test without
        /// failing completely.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        bool CannotValidateTest(TestCase c);

        OperationOutcome Validate(ITypedElement instance, IResourceResolver? resolver, string? profile = null);

        OperationOutcome? GetExpectedOperationOutcome(IValidatorEnginesResults engine);

        void SetOperationOutcome(IValidatorEnginesResults engine, OperationOutcome outcome);
    }
}
