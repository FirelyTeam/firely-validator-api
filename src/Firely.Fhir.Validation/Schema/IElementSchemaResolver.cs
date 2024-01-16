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
    /// An interface for objects that let you obtain an <see cref="ElementSchema"/> by its schema uri.
    /// </summary>
    public interface IElementSchemaResolver
    {
        /// <summary>
        /// Retrieve a schema by its schema uri.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>Returns null if the schema was not found.</returns>
        /// <exception cref="SchemaResolutionFailedException">Thrown when the schema was found, but could not be loaded or parsed.</exception>
        ElementSchema? GetSchema(Canonical schemaUri);
    }
}