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
    public class SchemaReferenceValidator : IGroupValidatable
    {
        /// <summary>
        /// A fixed uri that is to resolve the schema
        /// using a <see cref="IElementSchemaResolver" />.
        /// </summary>
        [DataMember]
        public Canonical SchemaUri { get; private set; }

        /// <summary>
        /// If set, this is the id of a subschema within the referenced schema
        /// that should be used to validate against, instead of the referenced 
        /// schema itself.
        /// </summary>
        [DataMember]
        public string? Subschema { get; private set; }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> for a fixed uri.
        /// </summary>
        public SchemaReferenceValidator(Canonical schemaUri, string? subschema = null)
        {
            SchemaUri = schemaUri;
            Subschema = subschema;
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, string, ValidationContext, ValidationState)" />
        public ResultAssertion Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var location = input.FirstOrDefault()?.Location;

            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            // Resolve the uri.
            var schema = vc.ElementSchemaResolver!.GetSchema(SchemaUri);

            if (schema is null)
                return new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE,
                   groupLocation, $"Unable to resolve reference to profile '{SchemaUri}'."));

            // If there is a subschema set, try to locate it.
            if (Subschema is not null)
            {
                var subschema = schema.FindFirstByAnchor(Subschema);
                if (subschema is null)
                    return new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE,
                       groupLocation, $"Unable to locate anchor {Subschema} within profile '{SchemaUri}'."));

                schema = subschema;
            }

            // Finally, validate
            return schema.Validate(input, groupLocation, vc, state);

        }

        /// <inheritdoc/>
        public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState state) => Validate(new[] { input }, input.Location, vc, state);


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            return new JProperty("ref", buildRef());

            string buildRef()
            {
                var baseRef = SchemaUri.ToString();
                if (Subschema is not null) baseRef += $", subschema {Subschema}";
                return baseRef;
            }
        }
    }
}
