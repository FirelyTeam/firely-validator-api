using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using T = System.Threading.Tasks;

namespace Firely.Validation.Compilation
{
    public class TerminologyServiceAdapter : ITerminologyServiceNEW
    {
        private readonly ITerminologyService _service;

        public TerminologyServiceAdapter(ITerminologyService service)
        {
            _service = service;
        }

        public async T.Task<Assertions> ValidateCode(string? canonical = null, string? context = null, string? code = null, string? system = null, string? version = null, string? display = null, Coding? coding = null, CodeableConcept? codeableConcept = null, FhirDateTime? date = null, bool? @abstract = null, string? displayLanguage = null)
        {
            var parameters = new ValidateCodeParameters()
               .WithValueSet(url: canonical, context: context)
               .WithCode(code: code, system: system, systemVersion: version, display: display, displayLanguage: displayLanguage)
               .WithCoding(coding)
               .WithCodeableConcept(codeableConcept)
               .WithAbstract(@abstract)
               .Build();

            var resultParms = await _service.ValueSetValidateCode(parameters);


            var result = resultParms.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = resultParms.GetSingleValue<FhirString>("message")?.Value;

            var assertions = Assertions.EMPTY;

            if (message is not null)
            {
                var severity = result ? OperationOutcome.IssueSeverity.Warning : OperationOutcome.IssueSeverity.Error;
                var issue = new IssueAssertion(-1, null, message, severity);
                assertions += issue;
            }

            return assertions;
        }
    }
}
