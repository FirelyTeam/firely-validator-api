/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// An interface for implementing a schema builder. Utilize this to extend the schema.
    /// </summary>
    internal interface ISchemaBuilder
    {
        /// <summary>
        /// Constucts a schema block.
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="conversionMode">The mode indicating the state we are in while constructing the schema block.</param>
        /// <returns></returns>
        IEnumerable<IAssertion> Build(
           ElementDefinitionNavigator nav,
           ElementConversionMode? conversionMode = ElementConversionMode.Full);
    }
}