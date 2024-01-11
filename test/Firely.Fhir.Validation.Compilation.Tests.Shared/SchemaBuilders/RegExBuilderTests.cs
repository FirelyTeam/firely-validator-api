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
