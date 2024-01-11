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

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class MaxLengthBuilderTests
    {
        private readonly MaxLengthBuilder _sut = new();

        [TestMethod]
        public void BuildTest()
        {
            var def = new ElementDefinition("path") { MaxLength = 10 };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.ContentReference);
            result.Should().BeEmpty();

            nav.MoveToFirstChild();
            result = _sut.Build(nav, ElementConversionMode.Full);
            result.Should().ContainSingle().Subject.Should().BeEquivalentTo(new MaxLengthValidator(10));
        }
    }
}
