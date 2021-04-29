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
    /// <summary>
    /// This specialized resolver contains corrections for the R3/R4 FHIR specification and
    /// applies them to the resolved StructureDefinitions. It should be used as a wrapper around resolvers for the
    /// core specification, and serve as input for a <see cref="SnapshotSource" />, before being cached.
    /// </summary>
    /// <remarks>This class is marked public since it is useful across the Firely products and 
    /// we recommend only using it if you are aware of the kind of corrections done by this resolver.</remarks>
    public class StructureDefinitionCorrectionsResolver : IAsyncResourceResolver, IResourceResolver
    {
#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Constructs a new correcting resolver.
        /// </summary>
        /// <param name="nested"></param>
        public StructureDefinitionCorrectionsResolver(ISyncOrAsyncResourceResolver nested)
        {
            Nested = nested.AsAsync();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The resolver for which the StructureDefinitions will be corrected.
        /// </summary>
        public IAsyncResourceResolver Nested { get; }

        /// <inheritdoc />
        public Resource? ResolveByCanonicalUri(string uri) => TaskHelper.Await(() => ResolveByCanonicalUriAsync(uri));

        /// <inheritdoc />
        public async Task<Resource?> ResolveByCanonicalUriAsync(string uri)
        {
            var result = await Nested.ResolveByCanonicalUriAsync(uri);
            return correctStructureDefinition(result);
        }

        private static Resource? correctStructureDefinition(Resource? result)
        {
            // If this is not a StructureDefinition, just pass it on without doing anything to it.
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

        /// <inheritdoc />
        public Resource? ResolveByUri(string uri) => ResolveByCanonicalUri(uri);

        /// <inheritdoc />
        public Task<Resource?> ResolveByUriAsync(string uri) => ResolveByCanonicalUriAsync(uri);
    }
}