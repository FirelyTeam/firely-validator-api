/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ReferencedInstanceValidatorTests : BasicValidatorDataAttribute
    {
        private static readonly ElementSchema SCHEMA = new("http://fixedschema",
            new IssueAssertion(0, "Validation was triggered", OperationOutcome.IssueSeverity.Information));

        public override IEnumerable<object?[]> GetData()
        {
            yield return [CreateInstance("#p1"), via(), true, null];
            yield return [CreateInstance("#p1"), via([AggregationMode.Contained]), true, null];
            yield return [CreateInstance("#p1"), via([AggregationMode.Bundled, AggregationMode.Contained]), true, null];
            yield return [CreateInstance("#p1"), via([AggregationMode.Bundled]), false, "which is not one of the allowed kinds"];
            yield return [CreateInstance("#p2"), via(), true, "Cannot resolve reference"];
            yield return [CreateInstance("Practitioner/3124"), via(), true, null];
            yield return [CreateInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Either), true, null];
            yield return [CreateInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Specific), false, "versioned reference but found"];
            yield return [CreateInstance("Practitioner/3124"), via(ver: ReferenceVersionRules.Independent), true, null];
            yield return [CreateInstance("https://example.com/base/Practitioner/3124"), via(), true, null];
            yield return [CreateInstance("Practitioner/3124"), via([AggregationMode.Bundled]), true, null];
            yield return [CreateInstance("Practitioner/3124"), via([AggregationMode.Contained]), false, "which is not one of the allowed kinds"];
            yield return [CreateInstance("Practitioner/3125"), via(), true, "Cannot resolve reference"];
            yield return [CreateInstance("http://example.com/hit"), via(), true, null];
            yield return [CreateInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Either), true, null];
            yield return [CreateInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Specific), true, null];
            yield return [CreateInstance("http://example.com/hit|3.0.1"), via(ver: ReferenceVersionRules.Independent), false, "versioned reference but found"];
            yield return [CreateInstance("http://example.com/hit"), via([AggregationMode.Bundled]), false, "which is not one of the allowed kinds"];
            yield return [CreateInstance("http://example.com/hit"), via([AggregationMode.Referenced]), true, null];
            yield return [CreateInstance("http://example.com/xhit"), via(), true, "Cannot resolve reference"];
        }

        private static ReferencedInstanceValidator via(AggregationMode[]? agg = null, ReferenceVersionRules? ver = null) => new(SCHEMA, agg, ver);
        
        public static object CreateInstance(string reference) =>
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
                            asserter = new { _type = "Reference", reference }
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

        private static ITypedElement? resolve(string url, string _) =>
            url.StartsWith("http://example.com/hit") ?
                (new { t = "irrelevant" }).DictionaryToTypedElement() : default;

        [ReferencedInstanceValidatorTests]
        [DataTestMethod]
        public void ValidateInstance(object instance, object testeeo, bool success, string? fragment)
        {
            ReferencedInstanceValidator testee = (ReferencedInstanceValidator)testeeo;

            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: new TestResolver() { SCHEMA });
            vc.ResolveExternalReference = resolve;

            var result = test(instance, testee, vc);

            if (success)
                result.SucceededWith(fragment ?? "Validation was triggered");
            else
                result.FailedWith(fragment ?? throw new InvalidOperationException("should have fragment"));

            static ResultReport test(object instance, IAssertion testee, ValidationSettings vc)
            {
                var te = instance.DictionaryToTypedElement().AsScopedNode();
                var asserter = te.Children("entry").First().Children("resource").Children("asserter").Single();
                return testee.Validate(asserter, vc);
            }
        }

        [TestMethod]
        public void ValidateInstanceViaCodeableReference()
        {
            var settings = ValidationSettings.BuildMinimalContext(schemaResolver: new TestResolver() { SCHEMA });
            settings.ResolveExternalReference = resolve;
            
            var instance = new CodeableReference { Reference = new ResourceReference("http://example.com/hit") };
            var validator = via();
            var result = validator.Validate(instance.ToTypedElement(), settings);

            result.SucceededWith("Validation was triggered");
        }
    }
}
