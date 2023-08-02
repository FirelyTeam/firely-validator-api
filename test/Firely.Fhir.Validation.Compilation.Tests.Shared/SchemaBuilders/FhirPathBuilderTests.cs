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
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation.Tests
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
            var builders = result.Should()
                .HaveCount(2).And
                .AllBeAssignableTo<InvariantValidator>().Which;

            builders.First().Should().BeOfType<FhirEle1Validator>();
            builders.Skip(1).First().Should().BeOfType<FhirExt1Validator>();
        }

        [TestMethod]
        public void SingleConstraintTest()
        {
            var constraint = new ConstraintComponent()
            {
                Key = "abc-1",
                ExpressionElement = new("'aaa'.length = 3"),
                Severity = ConstraintSeverity.Error,
                Human = "Single test",
            };
            constraint.AddExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice", new FhirBoolean(true));
            var def = new ElementDefinition("path")
            {
                Constraint = new() { constraint }
            };

            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav, ElementConversionMode.Full);
            result.Should()
                .ContainSingle().Subject
                .Should().BeEquivalentTo(new FhirPathValidator(
                    key: "abc-1",
                    expression: "'aaa'.length = 3",
                    humanDescription: "Single test",
                    severity: OperationOutcome.IssueSeverity.Error,
                    bestPractice: true));
        }
    }
}
