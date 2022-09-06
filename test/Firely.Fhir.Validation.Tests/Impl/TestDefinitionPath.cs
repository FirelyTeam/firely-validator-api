/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestDefinitionPath
    {
        [TestMethod]
        public void NavigateStart()
        {
            var testee = DefinitionPath.Start();
            testee.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void NavigateToProfile()
        {
            var elemtSchema = new ElementSchema("http://test.org/test");
            var fhirTypeSchema = new ResourceSchema(new StructureDefinitionInformation("http://test.org/resource", null, "Patient", StructureDefinitionInformation.TypeDerivationRule.Specialization, false));
            var fhirProfileSchema = new ResourceSchema(new StructureDefinitionInformation("http://test.org/resource-profile", null, "Patient", StructureDefinitionInformation.TypeDerivationRule.Constraint, false));

            var testee = DefinitionPath.Start().InvokeSchema(elemtSchema);
            testee.ToString().Should().Be("->http://test.org/test");

            testee = DefinitionPath.Start().InvokeSchema(fhirTypeSchema);
            testee.ToString().Should().Be("->Patient");

            testee = DefinitionPath.Start().InvokeSchema(fhirProfileSchema);
            testee.ToString().Should().Be("->Patient(http://test.org/resource-profile)");
        }

        [TestMethod]
        public void NavigateInsideProfile()
        {
            var elemtSchema = new ElementSchema("http://test.org/test");
            var testee = DefinitionPath
                .Start()
                .InvokeSchema(elemtSchema)
                .ToChild("a")
                .ToChild("b")
                .CheckSlice("s1")
                .ToChild("c");

            testee.ToString().Should().Be("->http://test.org/test.a.b[s1].c");
        }
    }
}
