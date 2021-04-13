/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    public class SchemaReferenceValidator : IValidatable
    {
        /// <summary>
        /// How this assertion will obtain a schema to validate the instance against
        /// </summary>
        public enum SchemaUriOrigin
        {
            /// <summary>
            /// Retrieve the schema directly from the static uri given in <see cref="SchemaUri"/>.
            /// </summary>
            Fixed,

            /// <summary>
            /// At runtime, use the type of the instance to retrieve the schema to validate against.
            /// </summary>
            /// <remarks>This assumes the canonical uri for the runtime type is directly resolvable as a
            /// schema uri.</remarks>
            RuntimeType,

            /// <summary>
            /// Use the path in <see cref="SchemaUriMember"/> to walk into an element of the instance 
            /// and retrieve the uri as a string from that element.
            /// </summary>
            InstanceMember
        }



#if MSGPACK_KEY
        /// <summary>
        /// How this assertion will obtain the schema to validate against.
        /// </summary>
        [DataMember(Order = 0)]
        public SchemaUriOrigin SchemaOrigin { get; private set; }

        /// <summary>
        /// A fixed uri that can be used to resolve the schema
        /// using a <see cref="IElementSchemaResolver" />. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.Fixed" />.
        /// </summary>
        [DataMember(Order = 1)]
        public Uri? SchemaUri { get; private set; }

        /// <summary>
        /// A path into and element in the instance, which the assertion will walk at runtime
        /// to fetch the uri from. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.InstanceMember" />.
        /// </summary>
        [DataMember(Order = 2)]
        public string? SchemaUriMember { get; private set; }

#else
        /// <summary>
        /// How this assertion will obtain the schema to validate against.
        /// </summary>
        [DataMember]
        public SchemaUriOrigin SchemaOrigin { get; private set; }

        /// <summary>
        /// A fixed uri that can be used to resolve the schema
        /// using a <see cref="IElementSchemaResolver" />. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.Fixed" />.
        /// </summary>
        [DataMember]
        public Uri? SchemaUri { get; private set; }

        /// <summary>
        /// A path into and element in the instance, which the assertion will walk at runtime
        /// to fetch the uri from. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.InstanceMember" />.
        /// </summary>
        [DataMember]
        public string? SchemaUriMember { get; private set; }

#endif

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> for a fixed uri.
        /// </summary>
        public SchemaReferenceValidator(Uri schemaUri) : this(schemaOrigin: SchemaUriOrigin.Fixed, schemaUri, null)
        {
            // nothing
        }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> for a fixed uri.
        /// </summary>
        public SchemaReferenceValidator(string schemaUri) : this(schemaOrigin: SchemaUriOrigin.Fixed,
            new Uri(schemaUri, UriKind.RelativeOrAbsolute), null)
        {
            // nothing
        }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> based on an instance member at runtime.
        /// </summary>
        public static SchemaReferenceValidator ForMember(string schemaUriMember) => new(schemaOrigin: SchemaUriOrigin.InstanceMember, null, schemaUriMember);

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> based on the runtime type of the instance.
        /// </summary>
        public static SchemaReferenceValidator ForRuntimeType() => new(schemaOrigin: SchemaUriOrigin.RuntimeType, null, null);

        // Deserialization constructor
        private SchemaReferenceValidator(SchemaUriOrigin schemaOrigin, Uri? schemaUri, string? schemaUriMember)
        {
            if (schemaOrigin == SchemaUriOrigin.InstanceMember && schemaUriMember is null)
                throw new ArgumentNullException(nameof(schemaUriMember));

            if (schemaOrigin == SchemaUriOrigin.Fixed && schemaUri is null)
                throw new ArgumentNullException(nameof(schemaUri));

            (SchemaUri, SchemaUriMember, SchemaOrigin) = (schemaUri, schemaUriMember, schemaOrigin);
        }

        /// <summary>
        /// Turn a datatype name into a schema uri.
        /// </summary>
        /// <remarks>Note how this ties the data type names strictly to a HL7-defined url for
        /// the schema's.</remarks>
        public static string MapTypeNameToFhirStructureDefinitionSchema(string typeName)
        {
            var typeNameUri = new Uri(typeName, UriKind.RelativeOrAbsolute);

            return typeNameUri.IsAbsoluteUri ? typeName : "http://hl7.org/fhir/StructureDefinition/" + typeName;
        }

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext)"/>
        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState vs)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            // Get the uri from whatever the SchemaOrigin is.
            var (uri, error) = getUri(input);

            // Failed to get the uri (at runtime) from the origin, return the reason
            if (error is not null) return new Assertions(error);

            // If there is no schema reference (i.e. Resource.meta.profile or Extension.url is empty)
            // this is perfectly fine.
            if (uri is null) return Assertions.SUCCESS;

            // A bit of a hack :-(  if this is a local uri from a complex FHIR Extension, this should
            // not be resolved, and just return success.  Actually, the compiler should handle this
            // and not generate a SchemaAssertion for these properties, but that is rather complex to
            // detect. I need to get this done now, will create a task for it to handle it correctly
            // later.
            if (SchemaOrigin == SchemaUriOrigin.InstanceMember && !uri.IsAbsoluteUri) return Assertions.SUCCESS;

            // Now, resolve the uri.
            var schema = await vc.ElementSchemaResolver!.GetSchema(uri).ConfigureAwait(false);

            if (schema is null)
                return new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                   input.Location, $"Unable to resolve reference to profile '{uri.OriginalString}'.")));

            // Finally, validate
            return await ValidationExtensions.Validate(schema, input, vc, vs).ConfigureAwait(false);
        }

        private (Uri? uri, ResultAssertion? error) getUri(ITypedElement input)
        {
            switch (SchemaOrigin)
            {
                case SchemaUriOrigin.RuntimeType:
                    {
                        // derive the schema to validate against from the (resource) type of the instance
                        if (input.InstanceType is null)
                            return (null, new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_ELEMENT_CANNOT_DETERMINE_TYPE,
                                    input.Location, $"The type of element {input.Location} is unknown, so it cannot be validated against its type only.")));

                        return (new Uri(MapTypeNameToFhirStructureDefinitionSchema(input.InstanceType)), null);
                    }
                case SchemaUriOrigin.InstanceMember:
                    {
                        // Constructor will have ensured there is a SchemaUriMember.
                        var uriFromMember = GetStringByMemberName(input, SchemaUriMember!);
                        return uriFromMember is not null ?
                            (new Uri(uriFromMember, UriKind.RelativeOrAbsolute), null) : (null, null);
                    }
                default:
                    return (SchemaUri!, null);
            }
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() =>
            new JProperty("$ref", SchemaUri?.ToString() ??
                (SchemaUriMember is not null ? $"(via {SchemaUriMember})" : "(via runtime type)"));

        /// <summary>
        /// Walks the path (the name of a direct child, or a path with '.' notation) into
        /// the given <see cref="ITypedElement" /> and returns the value of that element.
        /// </summary>
        /// <remarks>Returns the first of such members if there were more (either because
        /// the element repeats, or inner elements in the path repeat.</remarks>
        /// <returns><c>null</c> if the member was not found.</returns>
        public static string? GetStringByMemberName(ITypedElement ite, string path)
        {
            return path == "$this" ?
                ite.Value as string
                : navigatePath(ite, path).Take(1).Select(s => s.Value).OfType<string>().SingleOrDefault();

            IEnumerable<ITypedElement> navigatePath(ITypedElement input, string path)
            {
                var pathParts = path.Split('.');
                IEnumerable<ITypedElement> targets = new[] { input };

                return pathParts.Aggregate(targets, (ts, p) => ts.SelectMany(t => t.Children(p)));
            }
        }
    }
}
