/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implemented by assertions that work on a single ITypedElement.
    /// </summary>
    public interface IValidatable : IAssertion
    {
        /// <summary>
        /// Validates a single instance.
        /// </summary>
        ResultReport Validate(ROD input, ValidationContext vc, ValidationState state);
    }
}
