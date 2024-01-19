/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;
using System.ComponentModel;

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// An interface for implementing a schema builder. Utilize this to extend the schema.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public interface ISchemaBuilder
    {

        /// <summary>
        /// Constucts a schema block.
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="conversionMode">The mode indicating the state we are in while constructing the schema block.</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
        [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
        IEnumerable<IAssertion> Build(
           ElementDefinitionNavigator nav,
           ElementConversionMode? conversionMode = ElementConversionMode.Full);
    }
}