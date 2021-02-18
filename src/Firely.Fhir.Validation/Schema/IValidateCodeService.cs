/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */


using Hl7.Fhir.ElementModel.Types;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface for validating the two code-binding related types against a valueset.
    /// </summary>
    public interface IValidateCodeService
    {
        /// <summary>
        /// Validate a Concept against the content of a given valueset.
        /// </summary>
        /// <param name="valueSetUrl"></param>
        /// <param name="cc"></param>
        /// <param name="abstractAllowed">Determines whether an abstract code is an acceptable choice.</param>
        Task<CodeValidationResult> ValidateConcept(string valueSetUrl, Concept cc, bool abstractAllowed);

        /// <summary>
        /// Validate a Coding against the content of a given valueset.
        /// </summary>
        /// <param name="valueSetUrl"></param>
        /// <param name="code"></param>
        /// <param name="abstractAllowed">Determines whether an abstract code is an acceptable choice.</param>
        Task<CodeValidationResult> ValidateCode(string valueSetUrl, Code code, bool abstractAllowed);
    }

    /// <summary>
    /// The result of a call to the <see cref="IValidateCodeService" />
    /// </summary>
#pragma warning disable CS1591 // Compiler does not understand positional params in xmldoc yet.
    public record CodeValidationResult(bool Success, string? Message);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}