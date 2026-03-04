/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// This implementation of <see cref="IElementSchemaResolver"/> will resolve a schema from its underlying
    /// <see cref="Source"/> <see cref="IElementSchemaResolver"/>, unless this has been resolved before, in which
    /// case resolution is done immediately from a cache.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class CachedElementSchemaResolver : IElementSchemaResolver
    {
        private readonly ICache<Canonical, ElementSchema?> _cache;

        /// <summary>
        /// The <see cref="IElementSchemaResolver"/> used as the source for resolving schemas,
        /// as passed to the constructor.
        /// </summary>
        public IElementSchemaResolver Source { get; private set; }

        /// <summary>
        /// Constructs a caching resolver that uses it own cache to cache resolution calls to the 
        /// underlying <see cref="Source"/>.
        /// </summary>
        public CachedElementSchemaResolver(IElementSchemaResolver source)
            : this (source, new ConcurrentDictionaryCache<Canonical, ElementSchema?>())
        {
        }

        /// <summary>
        /// Constructs a caching resolver that uses an externally supplied cache to cache resolution calls to the 
        /// underlying <see cref="Source"/>.
        /// </summary>
        public CachedElementSchemaResolver(IElementSchemaResolver source, ICache<Canonical, ElementSchema?> externalCache)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _cache = externalCache;
        }
        
        /// <summary>
        /// Constructs a caching resolver that uses an externally supplied cache to cache resolution calls to the 
        /// underlying <see cref="Source"/>.
        /// </summary>
        public CachedElementSchemaResolver(IElementSchemaResolver source, ConcurrentDictionary<Canonical, ElementSchema?> externalCache)
            : this(source, new ConcurrentDictionaryCache<Canonical, ElementSchema?>(externalCache))
        {
        }

        /// <summary>
        /// Consults the cache for the schema, and if not present, uses the <see cref="Source"/> to retrieve
        /// a schema for the given uri.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved from the cache
        /// nor from the <see cref="Source"/>.</returns>
        public ElementSchema? GetSchema(Canonical schemaUri)
        {
            return _cache.GetOrAdd(schemaUri, static (uri, source) => source.GetSchema(uri), Source);
        }
    }
}