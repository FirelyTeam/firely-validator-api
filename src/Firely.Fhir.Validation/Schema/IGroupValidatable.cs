/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System.Collections.Generic;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The interface for a validation assertion that validates a rule about a set of elements.
    /// </summary>
    /// <remarks>A rule that validates cardinality is a great example of this kind of assertion.</remarks>
    internal interface IGroupValidatable : IAssertion, IValidatable
    {
        /// <summary>
        /// Validates a set of instances, given a location representative for the group.
        /// </summary>
        ResultReport Validate(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state);
    }
}
