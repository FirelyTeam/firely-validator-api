/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implemented by assertions that work on a single <see cref="IScopedNode"/>.
    /// </summary>
    internal interface IValidatable : IAssertion
    {
        /// <summary>
        /// Validates a single instance.
        /// </summary>
        ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state);
    }
}
