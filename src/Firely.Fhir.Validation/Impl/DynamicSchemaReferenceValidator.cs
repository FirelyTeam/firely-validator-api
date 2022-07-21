/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

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
        /// <summary>
        /// A path into and element in the instance, which the assertion will walk at runtime
        /// to fetch the uri from.
        /// </summary>
        [DataMember]
        public string SchemaUriMember { get; private set; }

        /// <summary>
        /// The schema to use when the member indicated by <see cref="SchemaUriMember"/> 
        /// is not present in the instance.
        /// </summary>
        [DataMember]
        public Canonical? DefaultSchema { get; private set; }

        /// <summary>
        /// Schemas to validate the instance against, irrespective of the contents
        /// of <see cref="SchemaUriMember"/> or <see cref="DefaultSchema"/>
        /// </summary>
        [DataMember]
        public Canonical[]? AdditionalSchemas { get; private set; }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> based on an instance member at runtime.
        /// </summary>
        public DynamicSchemaReferenceValidator(string schemaUriMember) : this(schemaUriMember, null, null)
        {
            // nothing
        }

        /// <summary>
        /// Construct a <see cref="SchemaReferenceValidator"/> based on an instance member at runtime.
        /// </summary>
        public DynamicSchemaReferenceValidator(string schemaUriMember, Canonical? defaultSchema, Canonical[]? additionalSchemas)
        {
            SchemaUriMember = schemaUriMember;
            DefaultSchema = defaultSchema;
            AdditionalSchemas = additionalSchemas;
        }


        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext, ValidationState)"/>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState vs)
        {
            var schemasToRun = new List<Canonical>();

            // Walk the path in SchemaUriMember and get the uri.
            var schemaUri = GetStringByMemberName(input, SchemaUriMember!) is string us ? new Canonical(us) : null;

            // If there is no schema reference (i.e. Resource.meta.profile or Extension.url is empty), 
            // run the default schema.
            if (schemaUri is null)
            {
                if (DefaultSchema is not null) schemasToRun.Add(DefaultSchema);
            }
            else
            {
                // TODO: A bit of a hack :-(  if this is a local uri from a complex FHIR Extension, this should
                // not be resolved, and just return success.  Actually, the compiler should handle this
                // and not generate a SchemaAssertion for these properties, but that is rather complex to
                // detect. I need to get this done now, will create a task for it to handle it correctly
                // later.
                if (!schemaUri!.IsAbsolute) return ResultReport.SUCCESS;

                schemasToRun.Add(schemaUri!);
            }

            // Always also run the additional schema's
            if (AdditionalSchemas is not null) schemasToRun.AddRange(AdditionalSchemas);

            // Validate the instance against the schema(s) using a SchemaReferenceValidator
            var schemaValidators = schemasToRun.Select(str => new SchemaReferenceValidator(str)).ToList();

            if (schemaValidators.Count == 1)
            {
                return schemaValidators.Single().ValidateOne(input, vc, vs);
            }
            else
            {
                var combined = new AllValidator(schemaValidators);
                return combined.ValidateOne(input, vc, vs);
            }
        }


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            return new JProperty("dynaref", buildContents());

            JToken buildContents()
            {
                if (AdditionalSchemas is null && DefaultSchema is null)
                {
                    return new JValue(SchemaUriMember);
                }
                else
                {
                    var contents = new JObject(
                        new JProperty("member", SchemaUriMember));
                    if (AdditionalSchemas?.Any() == true)
                        contents.Add(new JProperty("additional", string.Join(',', (IEnumerable<Canonical>)AdditionalSchemas)));
                    if (DefaultSchema is not null)
                        contents.Add(new JProperty("default", DefaultSchema.ToString()));

                    return contents;
                }
            }
        }

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
