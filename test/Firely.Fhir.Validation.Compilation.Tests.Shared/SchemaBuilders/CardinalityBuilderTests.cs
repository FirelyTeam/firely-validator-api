/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class CardinalityBuilderTests
    {
        private readonly CardinalityBuilder _sut = new();

        [TestMethod]
        public void BuildForContentReferenceTest()
        {
            var def = new ElementDefinition("path") { Min = 1 };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.ContentReference);
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void BuildCardinalityForRootTest()
        {
            var def = new ElementDefinition("root") { Min = 3, Max = "*" };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.Full);
            result.Should().BeEmpty();

        }

        [DataTestMethod]
        [DataRow(null, null, null, null, true)]
        [DataRow(null, "*", null, null)]
        [DataRow(null, "1", null, 1)]
        [DataRow(1, "*", 1, null)]
        [DataRow(1, "3", 1, 3)]
        public void BuildForAllTest(int? min, string? max, int? expectedMin, int? exptectedMax, bool shouldBeEmpty = false)
        {
            var def = new ElementDefinition("Resource.path") { Min = min, Max = max };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.Full);
            if (shouldBeEmpty)
            {
                result.Should().BeEmpty();
            }
            else
            {
                result.Should().ContainSingle().Subject.Should().BeEquivalentTo(new CardinalityValidator(expectedMin, exptectedMax));
            }
        }


    }
}
