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
    /// Asserts the validity of an extension using the url element in the extension instance.
    /// </summary>
    [DataContract]
    public class ExtensionAssertion : IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public Uri Reference { get; private set; }
#else
        [DataMember]
        public Uri Reference { get; private set; }
#endif
        public ExtensionAssertion(string reference)
        {
            Reference = createUri(reference);
        }

        public ExtensionAssertion(Uri reference)
        {
            Reference = reference;
        }

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            if (vc.ElementSchemaResolver is null)
            {
                return new Assertions(ResultAssertion.CreateFailure(new IssueAssertion(
                          Issue.PROCESSING_CATASTROPHIC_FAILURE, null,
                          $"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.")));
            }

            var groups = input.GroupBy(elt => elt.Children("url").GetString());

            var result = Assertions.EMPTY;

            foreach (var item in groups)
            {
                Uri uri = createUri(item.Key);

                result += await ValidationExtensions.Validate(uri, item, vc);
            }

            return result.AddResultAssertion();
        }

        private Uri createUri(string? item)
            => Uri.TryCreate(item, UriKind.RelativeOrAbsolute, out var uri) ? (uri.IsAbsoluteUri ? uri : Reference) : Reference;

        public JToken ToJson() => new JProperty("$extension", Reference?.ToString() ??
            throw Error.InvalidOperation("Cannot convert to Json: reference refers to a schema without an identifier"));
    }
}
