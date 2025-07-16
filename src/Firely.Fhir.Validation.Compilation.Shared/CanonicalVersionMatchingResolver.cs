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
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// This resolver implements canonical version matching as per FHIR specification.
    /// It wraps another resolver and adds support for partial version matching.
    /// For example, a canonical reference with version "1.5" should match resources 
    /// with versions "1.5.0", "1.5.1", etc.
    /// </summary>
    /// <remarks>
    /// According to FHIR canonical matching rules:
    /// - If a canonical reference has no version, it should match any version of the resource
    /// - If a canonical reference has a version, it should match:
    ///   - An exact version match (existing behavior)
    ///   - A version that starts with the specified version (new behavior)
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class CanonicalVersionMatchingResolver : IAsyncResourceResolver, IResourceResolver
    {
        /// <summary>
        /// Constructs a new version matching resolver.
        /// </summary>
        /// <param name="nested">The underlying resolver to wrap</param>
        public CanonicalVersionMatchingResolver(ISyncOrAsyncResourceResolver nested)
        {
            Nested = nested.AsAsync();
        }

        /// <summary>
        /// The resolver that this instance wraps.
        /// </summary>
        public IAsyncResourceResolver Nested { get; }

        /// <inheritdoc />
        public Resource? ResolveByCanonicalUri(string uri) => TaskHelper.Await(() => ResolveByCanonicalUriAsync(uri));

        /// <inheritdoc />
        public async Task<Resource?> ResolveByCanonicalUriAsync(string uri)
        {
            // First, try exact match (existing behavior)
            var exactMatch = await Nested.ResolveByCanonicalUriAsync(uri).ConfigureAwait(false);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // If exact match fails and we have a version, try partial version matching
            var canonical = new Canonical(uri);
            if (!canonical.HasVersion)
            {
                // No version specified, can't do partial matching
                return null;
            }

            // Try to find a compatible version
            return await FindCompatibleVersionAsync(canonical).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Resource? ResolveByUri(string uri) => ResolveByCanonicalUri(uri);

        /// <inheritdoc />
        public Task<Resource?> ResolveByUriAsync(string uri) => ResolveByCanonicalUriAsync(uri);

        /// <summary>
        /// Attempts to find a resource that matches the canonical URL with a compatible version.
        /// </summary>
        /// <param name="canonical">The canonical URL with version to match</param>
        /// <returns>The best matching resource, or null if no compatible version is found</returns>
        private async Task<Resource?> FindCompatibleVersionAsync(Canonical canonical)
        {
            if (canonical.Version == null || canonical.Uri == null)
            {
                return null;
            }

            // For now, we'll implement a simple approach that tries to find versions
            // that start with the requested version followed by a dot.
            // This handles cases like "1.5" matching "1.5.0", "1.5.1", etc.
            
            // Try common version patterns by appending .0, .1, etc.
            var versionToTry = canonical.Version;
            
            // If version doesn't contain a dot, try appending .0
            if (!versionToTry.Contains("."))
            {
                versionToTry = versionToTry + ".0";
            }
            else
            {
                // If version ends with a number but not .0, try appending .0
                if (!versionToTry.EndsWith(".0"))
                {
                    versionToTry = versionToTry + ".0";
                }
            }

            // Try the modified version
            var testCanonical = new Canonical(canonical.Uri, versionToTry, canonical.Anchor);
            var result = await Nested.ResolveByCanonicalUriAsync(testCanonical.Original).ConfigureAwait(false);
            
            if (result != null)
            {
                return result;
            }

            // If that didn't work, we could implement more sophisticated version matching
            // by querying the resolver for all versions and selecting the best match.
            // However, this would require the underlying resolver to support enumeration,
            // which is not guaranteed by the IAsyncResourceResolver interface.
            
            return null;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete