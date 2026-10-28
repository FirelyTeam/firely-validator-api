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

        private static IEnumerable<IAssertion> build(ElementDefinition def)
        {
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();
            return _sutStatic.Build(nav, ElementConversionMode.Full);
        }

        private static readonly MaxLengthBuilder _sutStatic = new();

        private static ElementDefinition systemStringElement(string? fhirType)
        {
            var typeRef = new ElementDefinition.TypeRefComponent { Code = "http://hl7.org/fhirpath/System.String" };
            if (fhirType is not null)
                typeRef.AddExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type", new FhirUrl(fhirType));
            return new ElementDefinition("Element.id") { Type = new() { typeRef } };
        }

        [TestMethod]
        public void SystemStringThatIsAFhirStringGetsMaxLength()
        {
            build(systemStringElement("string")).Should().ContainSingle()
                .Subject.Should().BeEquivalentTo(new MaxLengthValidator(1048576));
        }

        [TestMethod]
        public void SystemStringWithOtherFhirTypeGetsNoMaxLength()
        {
            build(systemStringElement("uri")).Should().BeEmpty();
            build(systemStringElement("id")).Should().BeEmpty();
            build(systemStringElement(null)).Should().BeEmpty();
        }

        [TestMethod]
        public void ExplicitMaxLengthWinsOverTheFhirStringLimit()
        {
            var def = systemStringElement("string");
            def.MaxLength = 20;
            build(def).Should().ContainSingle().Subject.Should().BeEquivalentTo(new MaxLengthValidator(20));
        }
    }
}
