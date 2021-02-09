using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class ExtensionAssertion : IGroupValidatable
    {
        private readonly Uri _referencedUri;

        public ExtensionAssertion(Uri reference)
        {
            _referencedUri = reference;
        }

        public Uri? ReferencedUri => _referencedUri;

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

                result += await ValidationExtensions.Validate(vc.ElementSchemaResolver.GetSchema, uri, item, vc);
            }

            return result.AddResultAssertion();
        }

        private Uri createUri(string? item)
            => Uri.TryCreate(item, UriKind.RelativeOrAbsolute, out var uri) ? (uri.IsAbsoluteUri ? uri : _referencedUri) : _referencedUri;

        public JToken ToJson() => new JProperty("$extension", ReferencedUri?.ToString() ??
            throw Error.InvalidOperation("Cannot convert to Json: reference refers to a schema without an identifier"));
    }
}
