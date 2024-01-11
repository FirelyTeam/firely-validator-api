/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class TestInstancePath
    {
        [TestMethod]
        public void NavigateStart()
        {
            var testee = InstancePath.Start();
            testee.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void NavigateToChildIndex()
        {
            var testee = InstancePath.Start()
                .StartResource("Patient")
                .ToChild("name")
                .ToIndex(3);
            testee.ToString().Should().Be("Patient.name[3]");
        }

        [TestMethod]
        public void NavigateWithOriginalIndices()
        {
            var testee = InstancePath.Start()
                .StartResource("Patient")
                .ToChild("identifier")
                .AddOriginalIndices(new[] { 0, 4, 7, 8 })
                .ToIndex(3);
            testee.ToString().Should().Be("Patient.identifier[8]");
        }

        [TestMethod]
        public void NavigateToInternalReference()
        {
            var testee = InstancePath.Start()
               .StartResource("Bundle")
               .ToChild("entry")
               .ToIndex(3)
               .ToChild("resource")
               .ToChild("name")
               .AddInternalReference("Bundle.entry[29]")
               .ToIndex(2)
               .ToChild("resource")
               .ToChild("name")
               .ToIndex(1);
            testee.ToString().Should().Be("Bundle.entry[2].resource.name[1]");
        }

        [TestMethod]
        public void NavigateToChoiceType()
        {
            var testee = InstancePath.Start()
                .StartResource("Observation")
                .ToChild("value[x]", "Quantity")
                .ToIndex(0);
            testee.ToString().Should().Be("Observation.valueQuantity[0]");

            testee = InstancePath.Start()
               .StartResource("Observation")
               .ToChild("value[x]", "string")
               .ToIndex(25);
            testee.ToString().Should().Be("Observation.valueString[25]");
        }
    }
}
