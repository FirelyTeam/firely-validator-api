﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Determines how to deal with FhirPath invariants marked as "best practice".
    /// </summary>
    /// <remarks>See <see cref="FhirPathAssertion.BestPractice"/> and https://www.hl7.org/fhir/best-practices.html </remarks>
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