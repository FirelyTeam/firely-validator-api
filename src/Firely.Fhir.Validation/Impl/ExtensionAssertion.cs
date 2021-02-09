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
    [DataContract]
    public class ExtensionAssertion : IGroupValidatable
    {
        [DataMember(Order = 0)]
        public Uri ReferencedUri { get; private set; }

        public ExtensionAssertion(Uri reference)
        {
            ReferencedUri = reference;
        }

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            if (vc.ElementSchemaResolver is null)
            {
                return Assertions.EMPTY + ResultAssertion.CreateFailure(new IssueAssertion(
                          Issue.PROCESSING_CATASTROPHIC_FAILURE, null,
                          $"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver."));
            }

            var groups = input.GroupBy(elt => elt.Children("url").GetString());

            var result = Assertions.EMPTY;

            foreach (var item in groups)
            {
                Uri uri = createUri(item.Key);

                result += await ValidationExtensions.Validate(vc.ElementSchemaResolver, uri, item, vc);
            }

            return result.AddResultAssertion();
        }

        private Uri createUri(string? item)
            => Uri.TryCreate(item, UriKind.RelativeOrAbsolute, out var uri) ? (uri.IsAbsoluteUri ? uri : ReferencedUri) : ReferencedUri;

        public JToken ToJson() => new JProperty("$extension", ReferencedUri?.ToString() ??
            throw Error.InvalidOperation("Cannot convert to Json: reference refers to a schema without an identifier"));
    }
}
