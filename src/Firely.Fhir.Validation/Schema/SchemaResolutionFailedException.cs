﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents an error that occurs when a schema exists, but could not be returned. The reasons 
    /// are diverse, but examples are unparseable schema's (or underlying StructureDefinitions), specification version errors,
    /// duplicate canonical urls, etc. Implementors do not have to raise this exception, but may choose to return <c>null</c>
    /// as a result of <see cref="IElementSchemaResolver.GetSchema(Canonical)"/> instead.
    /// </summary>
    public class SchemaResolutionFailedException : System.Exception
    {
        /// <summary>
        /// The schema uri that was used to resolve the schema.
        /// </summary>
        public Canonical SchemaUri { get; set; }

        /// <summary>
        /// Construct an exception given the message and the schema uri.
        /// </summary>
        public SchemaResolutionFailedException(string message, Canonical schemaUri) : base(message)
        {
            SchemaUri = schemaUri;
        }

        /// <summary>
        /// Construct an exception given the message, the schema uri and the inner exception with details about the reason
        /// why the load failed.
        /// </summary>
        public SchemaResolutionFailedException(string message, Canonical schemaUri, System.Exception inner) : base(message, inner)
        {
            SchemaUri = schemaUri;
        }
    }
}