/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestResolverImplementations
    {
        [TestMethod]
        public void TestPrimitiveResolver()
        {
            var testee = new SystemNamespaceElementSchemaResolver();

            Assert.IsNotNull(getSchema(testee, "http://hl7.org/fhirpath/System.Any"));
            Assert.IsNotNull(getSchema(testee, "http://hl7.org/fhirpath/System.String"));
            var ratio = getSchema(testee, "http://hl7.org/fhirpath/System.Ratio");
            Assert.IsNotNull(ratio);
            Assert.IsInstanceOfType(ratio!.Members.Single(), typeof(ChildrenValidator));
        }

        [TestMethod]
        public void TestCachingResolver()
        {
            var dummy = new DummyCachedResolver("http://example.org");
            var testee = new CachedElementSchemaResolver(dummy);

            Assert.IsNotNull(getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(1, dummy.Hits);
            Assert.IsNotNull(getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(1, dummy.Hits);

            Assert.IsNull(getSchema(testee, "http://somewhereelse.org/bla"));
            Assert.AreEqual(2, dummy.Hits);
            Assert.IsNull(getSchema(testee, "http://somewhereelse.org/bla"));
            Assert.AreEqual(2, dummy.Hits);
        }

        [TestMethod]
        public void TestMultiResolver()
        {
            var dummyA = new DummyCachedResolver("http://dummyA.org");
            var dummyB = new DummyCachedResolver("http://dummyB.org");
            var testee = new MultiElementSchemaResolver(dummyA, dummyB);

            // Hit A
            Assert.IsNotNull(getSchema(testee, "http://dummyA.org/bla"));
            Assert.AreEqual(1, dummyA.Hits);
            Assert.AreEqual(0, dummyB.Hits);

            // Now try a non-hit
            Assert.IsNull(getSchema(testee, "http://example.org/bla"));
            Assert.AreEqual(2, dummyA.Hits);
            Assert.AreEqual(1, dummyB.Hits);

            // Hit B
            Assert.IsNotNull(getSchema(testee, "http://dummyB.org/bla"));
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

            public ElementSchema? GetSchema(Canonical schemaUri)
            {
                Hits += 1;

                return schemaUri.Original.StartsWith(Prefix)
                    ? new ElementSchema(schemaUri)
                    : null;
            }
        }

        private static ElementSchema? getSchema(IElementSchemaResolver resolver, string uri)
        {
            var returned = resolver.GetSchema(new Canonical(uri));

            if (returned is not null)
            {
                Assert.AreEqual(uri, (string)returned!.Id);
            }

            return returned;
        }
    }
}
