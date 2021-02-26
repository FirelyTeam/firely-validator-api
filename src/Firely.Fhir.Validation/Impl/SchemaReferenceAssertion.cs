/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */


using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the validity of an element against a schema. The schema to validate against may be specified by either
    /// giving a fixed url, or it can be derived from a child element of the element under validation.
    /// </summary>
    /// <remarks>
    /// Being able to derive the schema from the input is useful when the schema uri is only available at runtime,
    /// e.g. in Extension.url or Resource.meta.profile
    /// </remarks>
    [DataContract]
    public class SchemaReferenceAssertion : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public Uri ReferencedUri { get; private set; }

        [DataMember(Order = 1]
        public string SchemaUriMember { get; private set; }
#else
        [DataMember]
        public Uri? SchemaUri { get; private set; }

        [DataMember]
        public string? SchemaUriMember { get; private set; }
#endif


        public SchemaReferenceAssertion(Uri schemaUri) : this(schemaUri, null)
        {
            // nothing
        }

        public SchemaReferenceAssertion(string schemaUriMember) : this(null, schemaUriMember)
        {
            // nothing
        }

        public const string USE_RUNTIME_TYPE_AS_URI = "type().name";

        public static SchemaReferenceAssertion ForRuntimeType() => new SchemaReferenceAssertion(USE_RUNTIME_TYPE_AS_URI);

        // Deserialization constructor
        private SchemaReferenceAssertion(Uri? schemaUri, string? schemaUriMember) => (SchemaUri, SchemaUriMember) = (schemaUri, schemaUriMember);

        // Note how this ties the data type names strictly to a HL7-defined url for
        // the schema's.
        public static string MapTypeNameToFhirStructureDefinitionSchema(string typeName) =>
            "http://hl7.org/fhir/StructureDefinition/" + typeName;

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            Uri uri;

            if (SchemaUri is not null)
                uri = SchemaUri;
            else if (SchemaUriMember == USE_RUNTIME_TYPE_AS_URI)
            {
                // derive the schema to validate against from the (resource) type of the instance
                if (input.InstanceType is null)
                    return new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                            null, $"The type of element {input.Location} is unknown, so it cannot be validated against its type only.")));

                uri = new Uri(MapTypeNameToFhirStructureDefinitionSchema(input.InstanceType));
            }
            else
            {
                // Note that because of the constructor, either SchemaUri is set, or SchemaUriMember,
                // so this else covers all the other cases where SchemaUriMember should have given us
                // the schema to validate against.
                var uriFromMember = SchemaUriMember is not null ? GetStringByMemberName(input, SchemaUriMember) : null;

                if (uriFromMember is null)
                    return new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                    null, $"Cannot validate the element {input.Location}, because there is no uri present in " +
                            $"'{SchemaUriMember}' to validate the element against.")));

                uri = new Uri(uriFromMember);
            }

            if (uri.OriginalString.StartsWith("http://hl7.org/fhirpath/"))   // Compiler magic: stop condition
                return Assertions.SUCCESS;

            return await ValidationExtensions.Validate(uri, input, vc);
        }

        public JToken ToJson() =>
            new JProperty("$ref", SchemaUri?.ToString() ??
                (SchemaUriMember is not null ? $"(via {SchemaUriMember})" : "()"));

        public static string? GetStringByMemberName(ITypedElement ite, string name) =>
            name == "$this" ?
                ite.Value as string
                : ite.Children(name).Take(1).Select(s => s.Value).OfType<string>().SingleOrDefault();
    }
}
