using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests.Impl
{
    [TestClass]
    public class ValidateInstanceAssertionTests : SimpleAssertionDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[] { new Uri("http://someotherschema"), new SchemaReferenceAssertion(new Uri("http://someotherschema")) };
            yield return new object?[] { new Uri("http://extensionschema.nl"), new SchemaReferenceAssertion("url") };
        }

        [ValidateInstanceAssertionTests]
        [DataTestMethod]
        public async Task InvokesCorrectSchema(Uri schemaUri, SchemaReferenceAssertion testee)
        {
            var schema = new ElementSchema(schemaUri, new Children(true, ("value", new Fixed("hi"))));
            var resolver = new TestResolver() { schema };
            var vc = new ValidationContext { ElementSchemaResolver = resolver };

            var instance = new
            {
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
