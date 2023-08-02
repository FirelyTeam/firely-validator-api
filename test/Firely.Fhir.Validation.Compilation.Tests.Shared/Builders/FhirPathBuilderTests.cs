/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests.Shared.Builders
{
    [TestClass]
    public class FhirPathBuilderTests
    {
        private readonly FhirPathBuilder _sut = new();

        [TestMethod]
        public void BuildForBackboneComponentTest()
        {
            var def = new ElementDefinition("path")
            {
                Constraint = new()
                {
                    new()
                    {
                        Key = "ele-1"
                    }
                }
            };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.BackboneType);
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void HandcodedFPConstraintsTest()
        {
            var def = new ElementDefinition("path")
            {
                Constraint = new()
                {
                    new()
                    {
                        Key = "ele-1"
                    },
                    new()
                    {
                        Key = "ext-1"
                    }

                }
            };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.Full);
            result.Should()
                .HaveCount(2).And
                .AllBeAssignableTo<InvariantValidator>().Which.First().Should()
                .BeOfType<FhirEle1Validator>();
        }
    }
}
