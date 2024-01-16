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
    public class ContentReferenceBuilderTests
    {
        private readonly ContentReferenceBuilder _sut = new();

        [TestMethod]
        public void BuildWithNoContentReferenceTest()
        {
            var def = new ElementDefinition("path");
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav);
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void BuildWithChildrenTest()
        {
            var def = new ElementDefinition("path") { ContentReference = "contentReference" };
            var defChild = new ElementDefinition("path.child");
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def, defChild });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav);
            result.Should().BeEmpty();
        }


        [TestMethod]
        public void BuildWithFragmentReferenceTest()
        {
            var def = new ElementDefinition("path") { ContentReference = "#boolean.id" };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav);
            result
                .Should().ContainSingle().Which
                .Should().BeEquivalentTo(new SchemaReferenceValidator("http://hl7.org/fhir/StructureDefinition/boolean#boolean.id"));
        }

        [TestMethod]
        public void BuildWithContentReferenceTest()
        {
            var def = new ElementDefinition("path") { ContentReference = "http://hl7.org/fhir/StructureDefinition/boolean" };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav);
            result
                .Should().ContainSingle().Which
                .Should().BeEquivalentTo(new SchemaReferenceValidator("http://hl7.org/fhir/StructureDefinition/boolean"));
        }
    }
}
