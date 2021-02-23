/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using System;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface for objects that let you obtain an <see cref="IElementSchema"/> by its schema uri.
    /// </summary>
    public interface IElementSchemaResolver
    {
        /// <summary>
        /// Retrieve a schema by its schema uri.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>Returns null if the schema was not found.</returns>
        Task<IElementSchema?> GetSchema(Uri schemaUri);
    }
}