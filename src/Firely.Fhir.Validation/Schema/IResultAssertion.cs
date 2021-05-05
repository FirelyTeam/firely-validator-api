/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface implemented by validators and assertions that represent a validation result.
    /// </summary>
    public interface IResultAssertion : IAssertion
    {
        /// <summary>
        /// The <see cref="ValidationResult"/> represented by this instance.
        /// </summary>
        ValidationResult Result { get; }
    }

}