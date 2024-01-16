/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

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
