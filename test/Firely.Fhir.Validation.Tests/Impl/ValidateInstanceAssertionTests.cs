using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests.Impl
{
    [TestClass]
    public class ValidateInstanceAssertionTests : SimpleAssertionDataAttribute
    {
        public static IElementSchema schema = new ElementSchema(new Uri("http://fixedschema"),
            new ResultAssertion(ValidationResult.Success, new IssueAssertion(0, "Validation was triggered")));

        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[] { createInstance("#p1"), via(), null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Contained }), null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Bundled, AggregationMode.Contained }), null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Bundled }), "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("#p2"), via(), "Cannot resolve reference" };

            yield return new object?[] { createInstance("Practitioner/3124"), via(), null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Either), null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Specific), "versioned reference but found" };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Independent), null };
            yield return new object?[] { createInstance("https://example.com/base/Practitioner/3124"), via(), null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(new[] { AggregationMode.Bundled }), null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(new[] { AggregationMode.Contained }), "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("Practitioner/3125"), via(), "Cannot resolve reference" };

            yield return new object?[] { createInstance("http://example.com/hit"), via(), null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Either), null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Specific), null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Independent), "versioned reference but found" };
            yield return new object?[] { createInstance("http://example.com/hit"), via(new[] { AggregationMode.Bundled }), "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("http://example.com/hit"), via(new[] { AggregationMode.Referenced }), null };
            yield return new object?[] { createInstance("http://example.com/xhit"), via(), "Cannot resolve reference" };

            static ValidateInstanceAssertion via(AggregationMode[]? agg = null, ReferenceVersionRules? ver = null) =>
                new ValidateInstanceAssertion("reference", schema, agg, ver);
        }


        public static object createInstance(string reference) =>
            new
            {
                resourceType = "Bundle",
                entry = new object[]
                {
                    new
                    {
                        fullUrl = "https://example.com/base/Condition/3123",
                        resource = new
                        {
                            resourceType = "Condition",
                            contained = new[]
                            {
                                new {
                                        resourceType = "Practitioner",
                                        id = "p1",
                                    }
                            },
                            asserter = new { reference = reference }
                        }
                    },
                    new
                    {
                        fullUrl = "https://example.com/base/Practitioner/3124",
                        resource = new
                        {
                            resourceType = "Practitioner",
                        }
                    }
                }
            };

        [ValidateInstanceAssertionTests]
        [DataTestMethod]
        public async Task ValidateInstance(object instance, ValidateInstanceAssertion testee, string fragment)
        {
            static Task<ITypedElement?> resolve(string url) =>
                Task.FromResult(url.StartsWith("http://example.com/hit") ?
                        (new { t = "irrelevant" }).ToTypedElement() : default);

            var vc = new ValidationContext
            {
                ElementSchemaResolver = new TestResolver() { schema },
                ResolveExternalReferences = true,
                ResourceResolver = resolve
            };

            var result = await test(instance, testee, vc);

            if (fragment is null)
                result.SucceededWith("Validation was triggered");
            else
                result.FailedWith(fragment);

            static async Task<Assertions> test(object instance, IAssertion testee, ValidationContext vc)
            {
                var te = new ScopedNode2(instance.ToTypedElement());
                var asserter = te.Children("entry").First().Children("resource").Children("asserter").Single();
                return await testee.Validate(asserter, vc);
            }
        }
    }


    public static class ResultAssert
    {
        public static async Task Failed(this Task<Assertions> result) => (await result).Failed();
        public static void Failed(this Assertions result) => Assert.IsFalse(result.Result.IsSuccessful);

        public static async Task FailedWith(this Task<Assertions> result, string messageFragment) => (await result).FailedWith(messageFragment);
        public static async Task FailedWithJust(this Task<Assertions> result, string messageFragment) => (await result).FailedWithJust(messageFragment);

        public static void FailedWith(this Assertions result, string messageFragment)
        {
            Assert.IsTrue(!result.Result.IsSuccessful);
            var messages = result.Result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }
        public static void FailedWithJust(this Assertions result, string messageFragment)
        {
            var issues = result.Result.Evidence.OfType<IssueAssertion>();
            Assert.AreEqual(1, issues.Count());
            result.FailedWith(messageFragment);
        }

        public static async Task Succeeded(this Task<Assertions> result) => (await result).Succeeded();
        public static void Succeeded(this Assertions result) => Assert.IsTrue(result.Result.IsSuccessful);

        public static async Task SucceededWith(this Task<Assertions> result, string messageFragment) => (await result).SucceededWith(messageFragment);
        public static void SucceededWith(this Assertions result, string messageFragment)
        {
            Assert.IsTrue(result.Result.IsSuccessful);
            var messages = result.Result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }

    }
}
