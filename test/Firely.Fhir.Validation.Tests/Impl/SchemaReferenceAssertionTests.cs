using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests.Impl
{
    [TestClass]
    public class SchemaReferenceAssertionTests : SimpleAssertionDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[] { new Uri("http://someotherschema"), new SchemaAssertion(new Uri("http://someotherschema")) };
            yield return new object?[] { new Uri("http://extensionschema.nl"), new SchemaAssertion("url") };
        }

        [SchemaReferenceAssertionTests]
        [DataTestMethod]
        public async Task InvokesCorrectSchema(Uri schemaUri, SchemaAssertion testee)
        {
            var schema = new ElementSchema(schemaUri, new Children(true, ("value", new Fixed("hi"))));
            var resolver = new TestResolver() { schema };
            var vc = ValidationContext.BuildMinimalContext(schemaResolver: resolver);

            var instance = new
            {
                _type = "Extension",
                url = "http://extensionschema.nl",
                value = "hi"
            };

            var result = await testee.Validate(instance.ToTypedElement(), vc);
            Assert.IsTrue(result.Result.IsSuccessful);
            Assert.IsTrue(resolver.ResolvedSchemas.Contains(schemaUri));
            Assert.AreEqual(1, resolver.ResolvedSchemas.Count);
        }
    }
}
