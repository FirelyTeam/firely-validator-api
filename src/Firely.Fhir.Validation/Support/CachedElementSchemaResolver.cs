/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Firely.Validation
{
    /// <summary>
    /// This implementation of <see cref="IElementSchemaResolver"/> will resolve a schema from its underlying
    /// <see cref="Source"/> <see cref="IElementSchemaResolver"/>, unless this has been resolved before, in which
    /// case resolution is done immediately from a cache.
    /// </summary>
    public class CachedElementSchemaResolver : IElementSchemaResolver
    {
        private readonly ConcurrentDictionary<Uri, ElementSchema?> _cache = new();

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
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Constructs a caching resolver that uses an externally supplied cache to cache resolution calls to the 
        /// underlying <see cref="Source"/>.
        /// </summary>
        public CachedElementSchemaResolver(IElementSchemaResolver source, ConcurrentDictionary<Uri, ElementSchema?> externalCache)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _cache = externalCache;
        }

        /// <summary>
        /// Consults the cache for the schema, and if not present, uses the <see cref="Source"/> to retrieve
        /// a schema for the given uri.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved from the cache
        /// nor from the <see cref="Source"/>.</returns>
        public async Task<ElementSchema?> GetSchema(Uri schemaUri)
        {
            // Direct hit.
            if (_cache.TryGetValue(schemaUri, out ElementSchema? schema)) return schema;

            var newValue = await Source.GetSchema(schemaUri);

            // Note that, if we were pre-empted between the TryGetValue and here, we'll just
            // not use the new schema just retrieved, and use whatever the other
            // thread put in the cache. So, no lock needed (which is hard with async/await in 
            // this case).
            return _cache.GetOrAdd(schemaUri, newValue);
        }

        public void DumpCache()
        {
            foreach (var item in _cache)
            {
                Debug.WriteLine($"==== {item.Key} ====");
                Debug.WriteLine(item.Value?.ToJson() ?? "(no available)");
            }
        }
    }
}