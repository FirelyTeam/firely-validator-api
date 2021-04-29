/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation
{
    public class StructureDefinitionCorrectionsResolver : IAsyncResourceResolver, IResourceResolver
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public StructureDefinitionCorrectionsResolver(ISyncOrAsyncResourceResolver nested)
        {
            Nested = nested.AsAsync();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public IAsyncResourceResolver Nested { get; }

        public Resource? ResolveByCanonicalUri(string uri) => TaskHelper.Await(() => ResolveByCanonicalUriAsync(uri));

        public async Task<Resource?> ResolveByCanonicalUriAsync(string uri)
        {
            var result = await Nested.ResolveByCanonicalUriAsync(uri);
            return correctStructureDefinition(result);
        }

        private static Resource? correctStructureDefinition(Resource? result)
        {
            if (result is not StructureDefinition sd) return result;

            if (sd.Kind == StructureDefinition.StructureDefinitionKind.Resource)
            {
                correctIdElement(sd.Differential); correctIdElement(sd.Snapshot);
            }

            if (sd.Type == "string")
            {
                correctStringRegex(sd.Differential); correctStringRegex(sd.Snapshot);
            }

            return sd;

            static void correctIdElement(IElementList elements)
            {
                if (elements is null) return;

                var idElements = elements.Element.Where(e => Regex.IsMatch(e.Path, @"^[a-zA-Z]+\.id$"));
                if (idElements.Count() == 1 && idElements.Single().Type.Count == 1)
                {
                    idElements.Single().Type = new() { new ElementDefinition.TypeRefComponent { Code = "id" } };
                }
            }

            static void correctStringRegex(IElementList elements)
            {
                if (elements is null) return;

                var valueElement = elements.Element.Where(e => e.Path == "string.value");
                if (valueElement.Count() == 1 && valueElement.Single().Type.Count == 1)
                {
                    valueElement.Single().Type.Single().
                        SetStringExtension("http://hl7.org/fhir/StructureDefinition/regex", @"[\r\n\t\u0020-\uFFFF]*");
                }
            }
        }

        public Resource? ResolveByUri(string uri) => ResolveByCanonicalUri(uri);
        public Task<Resource?> ResolveByUriAsync(string uri) => ResolveByCanonicalUriAsync(uri);
    }
}