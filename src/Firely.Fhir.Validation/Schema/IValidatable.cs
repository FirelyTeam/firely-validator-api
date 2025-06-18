/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System.ComponentModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implemented by assertions that work on a single <see cref="ITypedElement"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public interface IValidatable : IAssertion
    {
        /// <summary>
        /// Validates a single instance.
        /// </summary>
        ResultReport Validate(ITypedElement input, ValidationSettings vc, ValidationState state);
    }
}
