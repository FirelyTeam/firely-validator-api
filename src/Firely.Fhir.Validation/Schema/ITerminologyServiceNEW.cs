/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */


using Hl7.Fhir.Model;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public interface ITerminologyServiceNEW
    {
        Task<Assertions> ValidateCode(string? canonical = null, string? context = null, string? code = null,
                    string? system = null, string? version = null, string? display = null,
                    Coding? coding = null, CodeableConcept? codeableConcept = null, FhirDateTime? date = null,
                    bool? @abstract = null, string? displayLanguage = null);
    }
}