/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ReferencedInstanceValidatorTests : BasicValidatorDataAttribute
    {
        private static readonly ElementSchema SCHEMA = new("http://fixedschema",
            new ResultAssertion(ValidationResult.Success, new IssueAssertion(0, "Validation was triggered")));

        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[] { createInstance("#p1"), via(), true, null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Contained }), true, null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Bundled, AggregationMode.Contained }), true, null };
            yield return new object?[] { createInstance("#p1"), via(new[] { AggregationMode.Bundled }), false, "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("#p2"), via(), true, "Cannot resolve reference" };

            yield return new object?[] { createInstance("Practitioner/3124"), via(), true, null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Either), true, null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Specific), false, "versioned reference but found" };
            yield return new object?[] { createInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Independent), true, null };
            yield return new object?[] { createInstance("https://example.com/base/Practitioner/3124"), via(), true, null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(new[] { AggregationMode.Bundled }), true, null };
            yield return new object?[] { createInstance("Practitioner/3124"), via(new[] { AggregationMode.Contained }), false, "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("Practitioner/3125"), via(), true, "Cannot resolve reference" };

            yield return new object?[] { createInstance("http://example.com/hit"), via(), true, null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Either), true, null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Specific), true, null };
            yield return new object?[] { createInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Independent), false, "versioned reference but found" };
            yield return new object?[] { createInstance("http://example.com/hit"), via(new[] { AggregationMode.Bundled }), false, "which is not one of the allowed kinds" };
            yield return new object?[] { createInstance("http://example.com/hit"), via(new[] { AggregationMode.Referenced }), true, null };
            yield return new object?[] { createInstance("http://example.com/xhit"), via(), true, "Cannot resolve reference" };

            static ReferencedInstanceValidator via(AggregationMode[]? agg = null, ReferenceVersionRules? ver = null) =>
                new("reference", SCHEMA, agg, ver);
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
                            asserter = new { reference }
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

        [ReferencedInstanceValidatorTests]
        [DataTestMethod]
        public async Task ValidateInstance(object instance, ReferencedInstanceValidator testee, bool success, string fragment)
        {
            static Task<ITypedElement?> resolve(string url) =>
                Task.FromResult(url.StartsWith("http://example.com/hit") ?
                        (new { t = "irrelevant" }).ToTypedElement() : default);

            var vc = ValidationContext.BuildMinimalContext(schemaResolver: new TestResolver() { SCHEMA });
            vc.ExternalReferenceResolver = resolve;

            var result = await test(instance, testee, vc);

            if (success)
                result.SucceededWith(fragment ?? "Validation was triggered");
            else
                result.FailedWith(fragment);

            static async Task<ResultAssertion> test(object instance, IAssertion testee, ValidationContext vc)
            {
                var te = new ScopedNode(instance.ToTypedElement());
                var asserter = te.Children("entry").First().Children("resource").Children("asserter").Single();
                return await testee.Validate(asserter, vc);
            }
        }
    }
}
