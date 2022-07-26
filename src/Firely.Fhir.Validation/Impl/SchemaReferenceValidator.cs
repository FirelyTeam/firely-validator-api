﻿/* 
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
    public class SchemaReferenceValidator : IGroupValidatable
    {
        /// <summary>
        /// A fixed uri that is to resolve the schema
        /// using a <see cref="IElementSchemaResolver" />.
        /// </summary>
        [DataMember]
        public Canonical SchemaUri { get; private set; }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> for a fixed uri.
        /// </summary>
        public SchemaReferenceValidator(Canonical schemaUri)
        {
            if (schemaUri.Uri is null) throw new ArgumentException("Canonical must contain a url, not just an anchor", nameof(schemaUri));
            SchemaUri = schemaUri;
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, string, ValidationContext, ValidationState)" />
        public ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var location = input.FirstOrDefault()?.Location;

            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            var (schema, error) = FetchSchema(SchemaUri, vc.ElementSchemaResolver, groupLocation);
            if (error is not null)
                return error;

            // Finally, validate
            return schema!.Validate(input, groupLocation, vc, state);
        }

        internal static (ElementSchema? schema, ResultReport? failureReport) FetchSchema(Canonical canonical, IElementSchemaResolver resolver, string location)
        {
            ResultReport makeUnresolvableError(string message) => new(ValidationResult.Undecided, new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE, location, message));

            var (coreSchema, version, anchor) = canonical;

            if (coreSchema is null)
                return (null, makeUnresolvableError($"Resolving to local anchors is unsupported: '{canonical}'."));

            // Resolve the uri - without the anchor part.
            if (resolver.GetSchema(new Canonical(coreSchema, version, null)) is not { } schema)
                return (null, makeUnresolvableError($"Unable to resolve reference to profile '{canonical}'."));

            // If there is a subschema set, try to locate it.
            if (anchor is not null)
            {
                if (schema.FindFirstByAnchor(anchor) is { } subschema)
                    return (subschema, null);
                else
                    return (null, makeUnresolvableError($"Unable to locate anchor {anchor} within profile '{canonical}'."));
            }
            else
                return (schema, null);
        }


        /// <inheritdoc/>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state) => Validate(new[] { input }, input.Location, vc, state);


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            return new JProperty("ref", buildRef());

            string buildRef()
            {
                return SchemaUri.ToString();
            }
        }
    }
}
