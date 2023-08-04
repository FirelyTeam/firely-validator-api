/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// An interface for implementing a schema builder. Utilize this to extend the schema.
    /// </summary>
    public interface ISchemaBuilder
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