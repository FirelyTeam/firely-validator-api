﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            yield return new object?[] { new Uri("http://hl7.org/fhir/StructureDefinition/Extension"), SchemaAssertion.ForRuntimeType() };
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

        [DataTestMethod]
        [DataRow("nonsense", null)]
        [DataRow("$this", "value")]
        [DataRow("child", "child")]
        [DataRow("child2", null)] // no _value
        [DataRow("child2.child3", "value3")]
        [DataRow("rep", null)] // no _value
        [DataRow("rep.child4", "value4a")]
        public void WalksInstanceCorrectly(string path, string? expected)
        {
            var instance = new
            {
                _value = "value",
                child = "child",
                child2 = new
                {
                    child3 = new
                    {
                        _value = "value3"
                    }
                },
                rep = new[] {
                    new { child4 = new { _value = "value4a" }},
                    new { child4 = new { _value = "value4b" }}
                }
            };

            var instanceTE = instance.ToTypedElement();
            Assert.AreEqual(SchemaAssertion.GetStringByMemberName(instanceTE, path), expected);
        }
    }
}
