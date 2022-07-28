/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel.Types;

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
        /// <param name="valueSetUrl">Value set Canonical URL.</param>
        /// <param name="cc">A full codeableConcept to validate</param>
        /// <param name="abstractAllowed">Determines whether an abstract code is an acceptable choice.</param>
        /// <param name="context">The context of the value set, so that the server can resolve this to a value set to validate against.</param>
        CodeValidationResult ValidateConcept(Canonical valueSetUrl, Concept cc, bool abstractAllowed, string? context = null);

        /// <summary>
        /// Validate a Coding against the content of a given valueset.
        /// </summary>
        /// <param name="valueSetUrl">Value set Canonical URL.</param>
        /// <param name="code">The code that is to be validated.</param>
        /// <param name="abstractAllowed">Determines whether an abstract code is an acceptable choice.</param>
        /// <param name="context">The context of the value set, so that the server can resolve this to a value set to validate against.</param>
        CodeValidationResult ValidateCode(Canonical valueSetUrl, Code code, bool abstractAllowed, string? context = null);
    }

    /// <summary>
    /// The result of a call to the <see cref="IValidateCodeService" />
    /// </summary>
#pragma warning disable CS1591 // Compiler does not understand positional params in xmldoc yet.
    public record CodeValidationResult(bool Success, string? Message);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}