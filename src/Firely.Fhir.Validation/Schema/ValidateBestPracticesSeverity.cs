/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Determines how to deal with FhirPath invariants marked as "best practice".
    /// </summary>
    /// <remarks>See <see cref="FhirPathValidator.BestPractice"/> and https://www.hl7.org/fhir/best-practices.html </remarks>
    public enum ValidateBestPracticesSeverity
    {
        /// <summary>
        /// When failing a best-practice FhirPath invariant, the result is interpreted as a <see cref="OperationOutcome.IssueSeverity.Warning" />.
        /// </summary>
        Warning,

        /// <summary>
        /// When failing a best-practice FhirPath invariant, the result is interpreted as a <see cref="OperationOutcome.IssueSeverity.Error" />.
        /// </summary>
        Error
    }
}
