﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
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
    public class SchemaAssertion : IValidatable
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
        [DataMember(Order = 0)]
        public SchemaUriOrigin SchemaOrigin { get; private set; }

        [DataMember(Order = 1)]
        public Uri SchemaUri { get; private set; }

        [DataMember(Order = 2)]
        public string SchemaUriMember { get; private set; }

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
        /// Construct a <see cref="SchemaAssertion"/> for a fixed uri.
        /// </summary>
        public SchemaAssertion(Uri schemaUri) : this(schemaOrigin: SchemaUriOrigin.Fixed, schemaUri, null)
        {
            // nothing
        }

        /// <summary>
        /// Construct a <see cref="SchemaAssertion"/> based on an instance member at runtime.
        /// </summary>
        public SchemaAssertion(string schemaUriMember) : this(schemaOrigin: SchemaUriOrigin.InstanceMember, null, schemaUriMember)
        {
            // nothing
        }

        /// <summary>
        /// Construct a <see cref="SchemaAssertion"/> based on the runtime type of the instance.
        /// </summary>
        public static SchemaAssertion ForRuntimeType() => new(schemaOrigin: SchemaUriOrigin.RuntimeType, null, null);

        // Deserialization constructor
        private SchemaAssertion(SchemaUriOrigin schemaOrigin, Uri? schemaUri, string? schemaUriMember)
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
        public static string MapTypeNameToFhirStructureDefinitionSchema(string typeName) =>
            "http://hl7.org/fhir/StructureDefinition/" + typeName;

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext)"/>
        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            Uri uri;

            switch (SchemaOrigin)
            {
                case SchemaUriOrigin.RuntimeType:
                    {
                        // derive the schema to validate against from the (resource) type of the instance
                        if (input.InstanceType is null)
                            return new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                                    null, $"The type of element {input.Location} is unknown, so it cannot be validated against its type only.")));

                        uri = new Uri(MapTypeNameToFhirStructureDefinitionSchema(input.InstanceType));
                        break;
                    }
                case SchemaUriOrigin.InstanceMember:
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
                        break;
                    }
                default:
                    {
                        uri = SchemaUri!;
                        break;
                    }
            }

            // TODO:
            // * Let the resolution for /fhirpath/ be done using another IElementSchema provider
            // * Are there enough details in the failure message? It would be nice to know the original
            // * schema uri which we validated against to mention in the error message (or trace?).
            return uri.OriginalString.StartsWith("http://hl7.org/fhirpath/")
                ? Assertions.SUCCESS
                : await ValidationExtensions.Validate(uri, input, vc);
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() =>
            new JProperty("$ref", SchemaUri?.ToString() ??
                (SchemaUriMember is not null ? $"(via {SchemaUriMember})" : "()"));

        /// <summary>
        /// Walks the path (the name of a direct child, or a path with '.' notation) into
        /// the given <see cref="ITypedElement" />.
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
