/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class FhirSchemaGroupAnalyzerTests
    {
        [TestMethod]
        public void MinimizesFamily()
        {
            var a = new Canonical("a");
            var aa = new Canonical("aa");
            var aaa = new Canonical("aaa");

            var pf1 = new ResourceSchema(new StructureDefinitionInformation(a, null, null!, null, false));
            var pf2 = new ResourceSchema(new StructureDefinitionInformation(aa, new[] { a }, null!, null, false));
            var pf3 = new ResourceSchema(new StructureDefinitionInformation(aaa, new[] { aa, a }, null!, null, false));

            var minimized = FhirSchemaGroupAnalyzer.CalculateMinimalSet(new[] { pf1, pf2, pf3 });
            minimized.Should().ContainSingle(s => s == pf3);
        }

        [TestMethod]
        public void MinimizesRelated()
        {
            var a = new Canonical("a");
            var aa = new Canonical("aa");
            var aaa = new Canonical("aaa");
            var aab = new Canonical("aab");
            var aabb = new Canonical("aabb");

            var pf1 = new ResourceSchema(new StructureDefinitionInformation(a, null, null!, null, false));
            var pf2 = new ResourceSchema(new StructureDefinitionInformation(aa, new[] { a }, null!, null, false));
            var pf3 = new ResourceSchema(new StructureDefinitionInformation(aaa, new[] { aa, a }, null!, null, false));
            var pf4 = new ResourceSchema(new StructureDefinitionInformation(aab, new[] { aa, a }, null!, null, false));
            var pf5 = new ResourceSchema(new StructureDefinitionInformation(aabb, new[] { aab, aa, a }, null!, null, false));

            var minimized = FhirSchemaGroupAnalyzer.CalculateMinimalSet(new[] { pf1, pf2, pf3, pf4, pf5 });
            minimized.Should().OnlyContain(s => s == pf3 || s == pf5).And.HaveCount(2);
        }

        [TestMethod]
        public void RetainsStrangers()
        {
            var a = new Canonical("a");
            var aa = new Canonical("aa");
            var c = new Canonical("c");
            var cc = new Canonical("cc");

            var pf1 = new ResourceSchema(new StructureDefinitionInformation(a, null, null!, null, false));
            var pf2 = new ResourceSchema(new StructureDefinitionInformation(aa, new[] { a }, null!, null, false));
            var pf3 = new ResourceSchema(new StructureDefinitionInformation(c, null, null!, null, false));
            var pf4 = new ResourceSchema(new StructureDefinitionInformation(cc, new[] { c }, null!, null, false));

            var minimized = FhirSchemaGroupAnalyzer.CalculateMinimalSet(new[] { pf1, pf2, pf3, pf4 });
            minimized.Should().OnlyContain(s => s == pf2 || s == pf4).And.HaveCount(2);
        }

        [TestMethod]
        public void RelatesInstanceAndDeclared()
        {
            var dataType = new StructureDefinitionInformation(new("dt"), null, "dt", StructureDefinitionInformation.TypeDerivationRule.Specialization, true);
            var dataTypeA = new StructureDefinitionInformation(new("dtA"), new[] { dataType.Canonical }, "dtA", StructureDefinitionInformation.TypeDerivationRule.Specialization, false);
            var dataTypeB = new StructureDefinitionInformation(new("dtB"), new[] { dataType.Canonical }, "dtB", StructureDefinitionInformation.TypeDerivationRule.Specialization, false);

            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(new ResourceSchema(dataTypeA), dataType.Canonical, null, new()));
            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(new ResourceSchema(dataTypeA), null, null, new()));
            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(null, dataType.Canonical, null, new()));

            var error = FhirSchemaGroupAnalyzer.ValidateConsistency(new ResourceSchema(dataTypeA), dataTypeB.Canonical, null, new());
            error.Result.Should().Be(ValidationResult.Failure);
            error.Evidence.Should().OnlyContain(e => e is IssueAssertion && ((IssueAssertion)e).IssueNumber == Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE.Code);
        }

        private static void straightSuccess(ResultReport r)
        {
            r.Result.Should().Be(ValidationResult.Success);
            r.Evidence.Should().BeEmpty();
        }

        private static void fails(ResultReport r, string because)
        {
            r.Result.Should().Be(ValidationResult.Failure);
            r.Evidence.Should().OnlyContain(e => e is IssueAssertion && ((IssueAssertion)e).Message.Contains(because));
        }

        [TestMethod]
        public void ChecksProfileConsistency()
        {
            var a = new Canonical("a");
            var aa = new Canonical("aa");
            var aab = new Canonical("aab");
            var c = new Canonical("c");

            var pf1 = new ResourceSchema(new StructureDefinitionInformation(a, null, "typeA", null, false));
            var pf2 = new ResourceSchema(new StructureDefinitionInformation(aa, new[] { a }, "typeA", null, false));
            var pf3 = new ResourceSchema(new StructureDefinitionInformation(c, null, "typeC", null, false));
            var pf5 = new ResourceSchema(new StructureDefinitionInformation(aab, new[] { aa, a }, null!, null, false));

            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(pf2, pf1.Url, new[] { pf5 }, new()));
            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(null, pf1.Url, new[] { pf5 }, new()));
            straightSuccess(FhirSchemaGroupAnalyzer.ValidateConsistency(null, pf2.Url, new[] { pf5 }, new()));
            fails(FhirSchemaGroupAnalyzer.ValidateConsistency(null, pf3.Url, new[] { pf5 }, new()), "incompatible");
            fails(FhirSchemaGroupAnalyzer.ValidateConsistency(pf3, null, new[] { pf5 }, new()), "incompatible");
        }
    }
}
