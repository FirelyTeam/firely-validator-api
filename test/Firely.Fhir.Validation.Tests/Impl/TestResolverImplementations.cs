/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestResolverImplementations
    {
        [TestMethod]
        public async Task TestPrimitiveResolver()
        {
            var testee = new SystemNamespaceElementSchemaResolver();

            Assert.IsNotNull(await getSchema(testee, "http://hl7.org/fhirpath/System.Any"));
            Assert.IsNotNull(await getSchema(testee, "http://hl7.org/fhirpath/System.String"));
            var ratio = await getSchema(testee, "http://hl7.org/fhirpath/System.Ratio");
            Assert.IsNotNull(ratio);
            Assert.IsInstanceOfType(ratio!.Members.Single(), typeof(ChildrenValidator));
        }

        [TestMethod]
        public async Task TestCachingResolver()
        {
            var dummy = new DummyCachedResolver("http://example.org");
            var testee = new CachedElementSchemaResolver(dummy);

            Assert.IsNotNull(await getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(1, dummy.Hits);
            Assert.IsNotNull(await getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(1, dummy.Hits);

            Assert.IsNull(await getSchema(testee, "http://somewhereelse.org/bla"));
            Assert.AreEqual(2, dummy.Hits);
            Assert.IsNull(await getSchema(testee, "http://somewhereelse.org/bla"));
            Assert.AreEqual(2, dummy.Hits);
        }

        [TestMethod]
        public async Task TestMultiResolver()
        {
            var dummyA = new DummyCachedResolver("http://dummyA.org");
            var dummyB = new DummyCachedResolver("http://dummyB.org");
            var testee = new MultiElementSchemaResolver(dummyA, dummyB);

            // Hit A
            Assert.IsNotNull(await getSchema(testee, "http://dummyA.org/bla"));
            Assert.AreEqual(1, dummyA.Hits);
            Assert.AreEqual(0, dummyB.Hits);

            // Now try a non-hit
            Assert.IsNull(await getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(2, dummyA.Hits);
            Assert.AreEqual(1, dummyB.Hits);

            // Hit B
            Assert.IsNotNull(await getSchema(testee, "http://dummyB.org/bla"));
            Assert.AreEqual(3, dummyA.Hits);
            Assert.AreEqual(2, dummyB.Hits);
        }

        internal class DummyCachedResolver : IElementSchemaResolver
        {
            public DummyCachedResolver(string prefix)
            {
                Prefix = prefix;
            }

            public int Hits;

            public string Prefix { get; }

            public Task<ElementSchema?> GetSchema(Canonical schemaUri)
            {
                Hits += 1;

                return schemaUri.Original.StartsWith(Prefix)
                    ? Task.FromResult<ElementSchema?>(new ElementSchema(schemaUri))
                    : Task.FromResult<ElementSchema?>(null);
            }
        }

        private static async Task<ElementSchema?> getSchema(IElementSchemaResolver resolver, string uri)
        {
            var returned = await resolver.GetSchema(new Canonical(uri));

            if (returned is not null)
            {
                Assert.AreEqual(uri, (string)returned!.Id);
            }

            return returned;
        }
    }
}
