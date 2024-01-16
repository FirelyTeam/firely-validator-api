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
                    },
                    new()
                    {
                        Key = "txt-1"
                    },
                    new()
                    {
                        Key = "txt-2"
                    },

                }
            };
            var nav = new ElementDefinitionNavigator(new List<ElementDefinition> { def });
            nav.MoveToFirstChild();

            var result = _sut.Build(nav, ElementConversionMode.Full);
            var builders = result.Should()
                .HaveCount(4).And
                .AllBeAssignableTo<InvariantValidator>().Which;

            builders.First().Should().BeOfType<FhirEle1Validator>();
            builders.Skip(1).First().Should().BeOfType<FhirExt1Validator>();
            builders.Skip(2).First().Should().BeOfType<FhirTxt1Validator>();
            builders.Skip(3).First().Should().BeOfType<FhirTxt2Validator>();
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
