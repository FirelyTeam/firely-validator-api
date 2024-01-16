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
    public class RegExBuilderTests
    {
        private readonly RegexBuilder _sut = new();

        [TestMethod]
        public void BuildForContentReferenceTest()
        {
            var def = new ElementDefinition("path");
            def.AddExtension("http://hl7.org/fhir/StructureDefinition/regex", new FhirString(".*"));
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.ContentReference);
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void MultipleRegExsTest()
        {
            var typeRef = new ElementDefinition.TypeRefComponent() { CodeElement = new FhirUri("string") };
            typeRef.AddExtension("http://hl7.org/fhir/StructureDefinition/regex", new FhirString("[1-9][0-9]*"));
            var def = new ElementDefinition("path")
            {
                Type = new() { typeRef }
            };
            def.AddExtension("http://hl7.org/fhir/StructureDefinition/regex", new FhirString("\\S*"));
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });

            nav.MoveToFirstChild();
            var result = _sut.Build(nav, ElementConversionMode.Full);
            result.Should().HaveCount(2).And.AllBeOfType<RegExValidator>();
        }
    }
}
