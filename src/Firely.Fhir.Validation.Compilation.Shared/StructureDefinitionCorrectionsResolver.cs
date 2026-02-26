/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
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
            var resource = await Nested.ResolveByCanonicalUriAsync(uri).ConfigureAwait(false);
            resource.Correct();
            return resource;
        }

        /// <inheritdoc />
        public Resource? ResolveByUri(string uri) => TaskHelper.Await(() => ResolveByUriAsync(uri));

        /// <inheritdoc />
        public async Task<Resource?> ResolveByUriAsync(string uri)
        {
            var resource = await Nested.ResolveByUriAsync(uri).ConfigureAwait(false);
            resource.Correct();
            return resource;
        }
    }
}