/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Validation
{
    /// <summary>
    /// This implementation of <see cref="IElementSchemaResolver"/> will resolve the schemas for the
    /// System/CQL datatypes used in FHIR, FhirPath, CQL and the mapping language. These are datatypes
    /// that have the form http://hl7.org/fhirpath/System.type, where "type" is one of the System types
    /// (i.e. System.Boolean, System.Ratio etc, see https://cql.hl7.org/09-b-cqlreference.html#types-2
    /// for the full list). These types are the "atomic" types StructureDefinitions in FHIR are based
    /// on, so themselves have no source definition to convert from.
    /// </summary>
    public class SystemNamespaceElementSchemaResolver : IElementSchemaResolver
    {
        // Note: we could move these to the SDK, but once our revision of the
        // type system is done, we can retrieve this using reflection too...
        internal const string SYSTEM_ANY_URI = "http://hl7.org/fhirpath/System.Any";
        internal const string SYSTEM_BOOLEAN_URI = "http://hl7.org/fhirpath/System.Boolean";
        internal const string SYSTEM_CODE_URI = "http://hl7.org/fhirpath/System.Code";
        internal const string SYSTEM_CONCEPT_URI = "http://hl7.org/fhirpath/System.Concept";
        internal const string SYSTEM_DATE_URI = "http://hl7.org/fhirpath/System.Date";
        internal const string SYSTEM_DATETIME_URI = "http://hl7.org/fhirpath/System.DateTime";
        internal const string SYSTEM_DECIMAL_URI = "http://hl7.org/fhirpath/System.Decimal";
        internal const string SYSTEM_LONG_URI = "http://hl7.org/fhirpath/System.Long";
        internal const string SYSTEM_INTEGER_URI = "http://hl7.org/fhirpath/System.Integer";
        internal const string SYSTEM_QUANTITY_URI = "http://hl7.org/fhirpath/System.Quantity";
        internal const string SYSTEM_RATIO_URI = "http://hl7.org/fhirpath/System.Ratio";
        internal const string SYSTEM_STRING_URI = "http://hl7.org/fhirpath/System.String";
        internal const string SYSTEM_TIME_URI = "http://hl7.org/fhirpath/System.Time";

        internal static readonly ReferenceAssertion STRING_SCHEMA_REF = new(SYSTEM_STRING_URI);

        // Definition of the schemas for the System types. Note that most
        // of them are empty, though there are actually some enforcable business
        // rules. We will add these later on.
        internal ElementSchema[] SYSTEM_SCHEMAS = new ElementSchema[]
        {
                new(SYSTEM_ANY_URI),  // mark as 'abstract' when this becomes possible
                new(SYSTEM_BOOLEAN_URI),
                new(SYSTEM_CODE_URI,
                    new Children(false,
                        ("code", STRING_SCHEMA_REF),
                        ("display", STRING_SCHEMA_REF),
                        ("system", STRING_SCHEMA_REF),
                        ("version", STRING_SCHEMA_REF)
                        )),
                new(SYSTEM_CONCEPT_URI,
                    new Children(false,
                        ("codes", new ReferenceAssertion(SYSTEM_CODE_URI)),
                        ("display", STRING_SCHEMA_REF)
                        )),
                new(SYSTEM_DATE_URI),
                new(SYSTEM_DATETIME_URI),
                new(SYSTEM_DECIMAL_URI),
                new(SYSTEM_LONG_URI),
                new(SYSTEM_INTEGER_URI),
                new(SYSTEM_QUANTITY_URI,
                    new Children(false,
                        ("value", new ReferenceAssertion(SYSTEM_DECIMAL_URI)),
                        ("unit", STRING_SCHEMA_REF)
                        )),
                new(SYSTEM_RATIO_URI,
                    new Children(false,
                        ("numerator", new ReferenceAssertion(SYSTEM_QUANTITY_URI)),
                        ("denominator", new ReferenceAssertion(SYSTEM_QUANTITY_URI))
                        )),
                new(SYSTEM_STRING_URI),
                new(SYSTEM_TIME_URI)
        };

        private readonly Dictionary<string, ElementSchema> _systemSchemaDictionary = new();

        /// <summary>
        /// Constructs a resolver for the System/CQL types.
        /// </summary>
        public SystemNamespaceElementSchemaResolver()
        {
            _systemSchemaDictionary = SYSTEM_SCHEMAS.ToDictionary(es => es.Id.OriginalString);
        }

        /// <summary>
        /// Consults the cache for the schema, and if not present, uses the <see cref="Source"/> to retrieve
        /// a schema for the given uri.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved from the cache
        /// nor from the <see cref="Source"/>.</returns>
        public Task<ElementSchema?> GetSchema(Uri schemaUri) =>
           Task.FromResult(_systemSchemaDictionary.TryGetValue(schemaUri.OriginalString, out var value) ? value : null);
    }
}