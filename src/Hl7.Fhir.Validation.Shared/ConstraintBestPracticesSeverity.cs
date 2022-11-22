/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

namespace Hl7.Fhir.Validation
{
    /// <summary>
    /// Represents whether to treat violations of the invariants marked as "best practice" as errors or as warnings.
    /// </summary>
    public enum ConstraintBestPracticesSeverity
    {
        /// <summary>
        /// A failed "Best Practice" invariant should result in a warning.
        /// </summary>
        Warning,

        /// <summary>
        /// A failed "Best Practice" invariant should result in an error.
        /// </summary>
        Error
    }
}