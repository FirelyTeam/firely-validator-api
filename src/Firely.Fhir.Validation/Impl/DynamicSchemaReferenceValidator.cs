/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
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
    /// Asserts the validity of an element against a schema indicated at run time in a child element 
    /// of the element under validation.
    /// </summary>
    /// <remarks>
    /// Being able to derive the schema from the input is useful when the schema uri is only available at runtime,
    /// e.g. in Extension.url or Resource.meta.profile
    /// </remarks>
    [DataContract]
    public class DynamicSchemaReferenceValidator : IValidatable
    {
#if MSGPACK_KEY
        /// <summary>
        /// A path into and element in the instance, which the assertion will walk at runtime
        /// to fetch the uri from. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.InstanceMember" />.
        /// </summary>
        [DataMember(Order = 0)]
        public string? SchemaUriMember { get; private set; }
#else
        /// <summary>
        /// A path into and element in the instance, which the assertion will walk at runtime
        /// to fetch the uri from. Only used when <see cref="SchemaOrigin" />
        /// is set to <see cref="SchemaUriOrigin.InstanceMember" />.
        /// </summary>
        [DataMember]
        public string? SchemaUriMember { get; private set; }
#endif
        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> based on an instance member at runtime.
        /// </summary>
        public DynamicSchemaReferenceValidator(string schemaUriMember) =>
            SchemaUriMember = schemaUriMember;

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext, ValidationState)"/>
        public async Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState vs)
        {
            // Walk the path in SchemaUriMember and get the uri.
            var schemaUri = GetStringByMemberName(input, SchemaUriMember!) is string us ?
                new Uri(us, UriKind.RelativeOrAbsolute) : null;

            // If there is no schema reference (i.e. Resource.meta.profile or Extension.url is empty)
            // this is perfectly fine.
            if (schemaUri is null) return ResultAssertion.SUCCESS;

            // A bit of a hack :-(  if this is a local uri from a complex FHIR Extension, this should
            // not be resolved, and just return success.  Actually, the compiler should handle this
            // and not generate a SchemaAssertion for these properties, but that is rather complex to
            // detect. I need to get this done now, will create a task for it to handle it correctly
            // later.
            if (!schemaUri.IsAbsoluteUri) return ResultAssertion.SUCCESS;

            // Validate the instance against the uri using a SchemaReferenceValidator
            var schemaValidatorInternal = new SchemaReferenceValidator(schemaUri);
            return await schemaValidatorInternal.Validate(input, vc, vs);
        }


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("dynaref", SchemaUriMember);

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

                // Target now contains a single child. Find the children for this target, and 
                // then collect the children of these children - one "step" deeper for each
                // path part.
                return pathParts.Aggregate(targets, (ts, p) => ts.SelectMany(t => t.Children(p)));
            }
        }
    }
}
