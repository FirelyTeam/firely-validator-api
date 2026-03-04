/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System;
using System.Collections.Concurrent;
using T = System.Threading.Tasks;
using Xunit;

#pragma warning disable CS0618 // CachedElementSchemaResolver is marked experimental/obsolete

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class CreateCachedResolverTests
    {
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>Minimal IAsyncResourceResolver that always returns null.</summary>
        private sealed class NullAsyncResourceResolver : IAsyncResourceResolver
        {
            public T.Task<Resource?> ResolveByUriAsync(string uri) => T.Task.FromResult<Resource?>(null);
            public T.Task<Resource?> ResolveByCanonicalUriAsync(string uri) => T.Task.FromResult<Resource?>(null);
        }

        /// <summary>A spy ICache that counts how many times GetOrAdd is called.</summary>
        private class SpyCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
        {
            private readonly ConcurrentDictionary<TKey, TValue> _inner = new();
            public int GetOrAddCallCount { get; private set; }

            public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            {
                GetOrAddCallCount++;
                return _inner.GetOrAdd(key, valueFactory);
            }

            public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArg)
            {
                GetOrAddCallCount++;
                return _inner.GetOrAdd(key, valueFactory, factoryArg);
            }
        }

        private static readonly Canonical TestUri = new("http://example.org/StructureDefinition/test");

        // -----------------------------------------------------------------------
        // Group 2 – StructureDefinitionToElementSchemaResolver.CreateCached
        // -----------------------------------------------------------------------

        /// <summary>Test 7: CreateCached with no external cache returns a CachedElementSchemaResolver.</summary>
        [Fact]
        public void CreateCached_WithNoExternalCache_ReturnsCachedResolver()
        {
            var asyncResolver = new NullAsyncResourceResolver();

            var result = StructureDefinitionToElementSchemaResolver.CreateCached(asyncResolver);

            result.Should().BeOfType<CachedElementSchemaResolver>();
        }

        /// <summary>Test 8: CreateCached with an external ICache uses that cache (spy counts calls).</summary>
        [Fact]
        public void CreateCached_WithExternalICache_UsesThatCache()
        {
            var asyncResolver = new NullAsyncResourceResolver();
            var spyCache = new SpyCache<Canonical, ElementSchema?>();

            var resolver = StructureDefinitionToElementSchemaResolver.CreateCached(asyncResolver, externalCache: spyCache);

            resolver.Should().BeOfType<CachedElementSchemaResolver>();

            // Trigger a couple of resolutions so the spy is invoked
            resolver.GetSchema(TestUri);
            resolver.GetSchema(TestUri);

            spyCache.GetOrAddCallCount.Should().BeGreaterThan(0,
                "the external cache should be invoked on every GetSchema call");
        }
    }
}

