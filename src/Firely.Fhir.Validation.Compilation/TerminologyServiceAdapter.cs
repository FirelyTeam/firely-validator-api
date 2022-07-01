/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Adapts an <see cref="ITerminologyService"/> to a <see cref="IValidateCodeService"/>
    /// for use with the validator.
    /// </summary>
    /// <remarks><c>ITerminologyService</c> is an interface representing the full list of
    /// functions described in the terminology surface in the FHIR specification. However,
    /// the validator only needs a specific subset (encoded in <c>IValidateCodeService</c>
    /// to function.</remarks>
    public class TerminologyServiceAdapter : IValidateCodeService
    {
        private readonly ITerminologyService _service;

        /// <summary>
        /// Initializes a new adapter that adapts the given service.
        /// </summary>
        /// <param name="service"></param>
        public TerminologyServiceAdapter(ITerminologyService service)
        {
            _service = service;
        }

        /// <inheritdoc />
        public CodeValidationResult ValidateCode(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Code code, bool abstractAllowed, string? context = null)
        {
            var parameters = new ValidateCodeParameters()
               .WithValueSet(url: (string)valueSetUrl)
               .WithCode(code: code.Value, system: code.System, systemVersion: code.Version, display: code.Display, context: context)
               .WithAbstract(abstractAllowed)
               .Build();

            return callService(parameters);
        }

        /// <inheritdoc />
        public CodeValidationResult ValidateConcept(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Concept cc, bool abstractAllowed)
        {
            var parameters = new ValidateCodeParameters()
               .WithValueSet(url: (string)valueSetUrl)
               .WithCodeableConcept(new CodeableConcept() { Text = cc.Display, Coding = cc.Codes?.Select(c => new Coding() { System = c.System, Code = c.Value, Display = c.Display, Version = c.Version }).ToList() })
               .WithAbstract(abstractAllowed)
               .Build();

            return callService(parameters);
        }

        private CodeValidationResult callService(Parameters parameters)
        {
            var resultParms = TaskHelper.Await(() => _service.ValueSetValidateCode(parameters));

            var result = resultParms.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = resultParms.GetSingleValue<FhirString>("message")?.Value;

            return new(result, message);
        }
    }
}
