/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the validity of an element against a fixed schema.
    /// </summary>
    [DataContract]
    public class Validator : IValidatable
    {
        /// <summary>
        /// A list of canonicals for the schemas to validate the instance against.
        /// </summary>
        [DataMember]
        public Canonical[] Schemas { get; private set; }

        /// <inheritdoc cref="Validator(IEnumerable{Canonical})" />
        public Validator(params Canonical[] schemaUris)
        {
            Schemas = schemaUris;
        }

        /// <summary>
        /// Construct a <see cref="Validator"/> given a set of canonicals for the
        /// schemas to validate against.
        /// </summary>
        public Validator(IEnumerable<Canonical> schemaUris)
        {
            Schemas = schemaUris.ToArray();
        }

        private static IEnumerable<(ElementSchema? schema, ResultReport? failureReport)> fetchSchemas(Canonical[] canonicals, IElementSchemaResolver resolver, string location) => canonicals.Select(c => SchemaReferenceValidator.FetchSchema(c, resolver, location));

        /// <inheritdoc/>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            var (typeSchema, typeSchemaError) = SchemaReferenceValidator.FetchSchema(Canonical.ForCoreType(input.InstanceType), vc.ElementSchemaResolver, input.Location);
            if (typeSchemaError is not null) return typeSchemaError;
            if (typeSchema is not FhirSchema) throw new InvalidOperationException("Resolving a base FHIR type should return (a subclass of) FhirSchema.");

            var additionalSchemas = typeSchema is ISchemaRedirector sdr ? sdr.GetRedirects(input) : Array.Empty<Canonical>();
            var allSchemasUris = additionalSchemas.Concat(Schemas).Distinct().ToArray();

            var allSchemas = fetchSchemas(allSchemasUris, vc.ElementSchemaResolver, input.Location);
            var allSchemaErrors = allSchemas.Where(s => s.failureReport is not null).Select(s => s.failureReport!);

            var fhirSchemas = allSchemas.Select(s => s.schema).OfType<FhirSchema>().ToArray();
            var fhirSchemaErrors = FhirSchemaGroupAnalyzer.ValidateConsistency(typeSchema as FhirSchema, null, fhirSchemas, input.Location);
            var minimalFhirSchemas = FhirSchemaGroupAnalyzer.CalculateMinimalSet(fhirSchemas);

            var minimalSchemas = minimalFhirSchemas.Concat(allSchemas.Select(s => s.schema).Where(s => s is not null));

            var allValidator = new AllValidator(minimalSchemas!);

            // Finally, validate
            var result = allValidator.Validate(input, vc, state);

            return ResultReport.FromEvidence(allSchemaErrors.Append(fhirSchemaErrors).Append(result).ToList());
        }


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            return new JProperty("validate", buildRef());

            string buildRef() => string.Join(',', Schemas.Select(s => s.ToString()));
        }
    }
}
