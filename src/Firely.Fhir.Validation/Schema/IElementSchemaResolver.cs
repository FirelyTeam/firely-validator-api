/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface for objects that let you obtain an <see cref="ElementSchema"/> by its schema uri.
    /// </summary>
    internal interface IElementSchemaResolver
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