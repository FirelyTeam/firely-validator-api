/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using System;
using System.Collections.Concurrent;
using Xunit;

#pragma warning disable CS0618 // CachedElementSchemaResolver is marked experimental/obsolete

namespace Firely.Fhir.Validation.Tests.Support
{
    public class CachedElementSchemaResolverTests
    {
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

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
        // Group 1 – CachedElementSchemaResolver behaviour
        // -----------------------------------------------------------------------

        /// <summary>Test 1: With no external cache the source is called only once per canonical.</summary>
        [Fact]
        public void DefaultConstructor_CachesResults()
        {
            var source = new TestResolver();
            var schema = new ElementSchema(TestUri);
            source.Add(schema);

            var sut = new CachedElementSchemaResolver(source);

            var first = sut.GetSchema(TestUri);
            var second = sut.GetSchema(TestUri);

            first.Should().BeSameAs(schema);
            second.Should().BeSameAs(schema);
            source.ResolvedSchemas.Should().HaveCount(1, "the source should only be consulted once");
        }

        /// <summary>Test 3: When constructed with an ICache implementation, all gets go through it.</summary>
        [Fact]
        public void ICache_Constructor_UsesSuppliedCache()
        {
            var source = new TestResolver();
            var schema = new ElementSchema(TestUri);
            source.Add(schema);

            var spyCache = new SpyCache<Canonical, ElementSchema?>();
            var sut = new CachedElementSchemaResolver(source, spyCache);

            sut.GetSchema(TestUri);
            sut.GetSchema(TestUri);

            spyCache.GetOrAddCallCount.Should().Be(2, "each call to GetSchema delegates to the cache");
            source.ResolvedSchemas.Should().HaveCount(1, "the source is only called for a cache miss");
        }

        /// <summary>Test 4: When constructed with a pre-populated ConcurrentDictionary, existing entries skip the source.</summary>
        [Fact]
        public void ConcurrentDictionary_Constructor_UsesSuppliedDictionary()
        {
            var prePopulatedSchema = new ElementSchema(TestUri);
            var existingCache = new ConcurrentDictionary<Canonical, ElementSchema?>();
            existingCache[TestUri] = prePopulatedSchema;

            var source = new TestResolver(); // source has no schemas – should not be called
            var sut = new CachedElementSchemaResolver(source, existingCache);

            var result = sut.GetSchema(TestUri);

            result.Should().BeSameAs(prePopulatedSchema);
            source.ResolvedSchemas.Should().BeEmpty("the entry was already in the supplied dictionary");
        }

        /// <summary>Test 5: When the source returns null, null is cached and the source is not called again.</summary>
        [Fact]
        public void GetSchema_ReturnsNullWhenSourceReturnsNull()
        {
            var source = new TestResolver(); // empty – always returns null
            var sut = new CachedElementSchemaResolver(source);

            var first = sut.GetSchema(TestUri);
            var second = sut.GetSchema(TestUri);

            first.Should().BeNull();
            second.Should().BeNull();
            source.ResolvedSchemas.Should().HaveCount(1, "null should be cached just like a real schema");
        }

        /// <summary>Test 6: Two resolvers sharing the same ICache benefit from each other's cached entries.</summary>
        [Fact]
        public void ExternalCache_IsSharedAcrossResolvers()
        {
            var schema = new ElementSchema(TestUri);

            var source1 = new TestResolver();
            source1.Add(schema);
            var source2 = new TestResolver(); // does NOT know about TestUri

            var sharedCache = new SpyCache<Canonical, ElementSchema?>();

            var resolver1 = new CachedElementSchemaResolver(source1, sharedCache);
            var resolver2 = new CachedElementSchemaResolver(source2, sharedCache);

            // Resolver 1 populates the shared cache
            var fromResolver1 = resolver1.GetSchema(TestUri);
            // Resolver 2 should find the entry already in the cache
            var fromResolver2 = resolver2.GetSchema(TestUri);

            fromResolver1.Should().BeSameAs(schema);
            fromResolver2.Should().BeSameAs(schema, "resolver2 should read the entry cached by resolver1");
            source1.ResolvedSchemas.Should().HaveCount(1);
            source2.ResolvedSchemas.Should().BeEmpty("resolver2 should never have to ask its source");
        }

    }
}

