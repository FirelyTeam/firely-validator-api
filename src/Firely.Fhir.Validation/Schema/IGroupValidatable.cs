/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The interface for a validation assertion that validates a rule about a set of elements.
    /// </summary>
    /// <remarks>A rule that validates cardinality is a great example of this kind of assertion.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public interface IGroupValidatable : IAssertion, IValidatable
    {
        /// <summary>
        /// Validates a set of instances, given a location representative for the group.
        /// </summary>
        ResultReport Validate(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state);

        /// <summary>
        /// Validates a set of instances, given a location representative for the group.
        /// </summary>
        ValueTask<ResultReport> ValidateAsync(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
            => new(Validate(input, vc, state));
    }
}
