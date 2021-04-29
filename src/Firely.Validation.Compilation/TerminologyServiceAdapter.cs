/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation
{
    public class TerminologyServiceAdapter : IValidateCodeService
    {
        private readonly ITerminologyService _service;

        public TerminologyServiceAdapter(ITerminologyService service)
        {
            _service = service;
        }

        public async Task<CodeValidationResult> ValidateCode(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Code code, bool abstractAllowed)
        {
            var parameters = new ValidateCodeParameters()
               .WithValueSet(url: (string)valueSetUrl)
               .WithCode(code: code.Value, system: code.System, systemVersion: code.Version, display: code.Display)
               .WithAbstract(abstractAllowed)
               .Build();

            return await callService(parameters);
        }

        public async Task<CodeValidationResult> ValidateConcept(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Concept cc, bool abstractAllowed)
        {
            var parameters = new ValidateCodeParameters()
               .WithValueSet(url: (string)valueSetUrl)
               .WithCodeableConcept(new CodeableConcept() { Text = cc.Display, Coding = cc.Codes?.Select(c => new Coding() { System = c.System, Code = c.Value, Display = c.Display, Version = c.Version }).ToList() })
               .WithAbstract(abstractAllowed)
               .Build();

            return await callService(parameters);
        }


        private async Task<CodeValidationResult> callService(Parameters parameters)
        {
            var resultParms = await _service.ValueSetValidateCode(parameters);

            var result = resultParms.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = resultParms.GetSingleValue<FhirString>("message")?.Value;

            return new(result, message);
        }
    }
}
