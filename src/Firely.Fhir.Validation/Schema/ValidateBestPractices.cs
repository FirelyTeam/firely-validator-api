/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Determines how to deal with FhirPath invariants marked as "best practice".
    /// </summary>
    /// <remarks>See <see cref="FhirPathValidator.BestPractice"/> and https://www.hl7.org/fhir/best-practices.html </remarks>
    public enum ValidateBestPractices
    {
        /// <summary>
        /// When failing a best-practice FhirPath invariant, the result is ignored.
        /// </summary>
        Ignore,

        /// <summary>
        /// When failing a best-practice FhirPath invariant, the result is interpreted as a <see cref="OperationOutcome.IssueSeverity.Error" />.
        /// </summary>
        Enabled,

        /// <summary>
        /// When failing a best-practice FhirPath invariant, the result is interpreted as a <see cref="OperationOutcome.IssueSeverity.Warning" />.
        /// </summary>
        Disabled
    }
}
