/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    internal class PatternValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            // integer
            yield return new object?[]
            {
                new PatternValidator(new Hl7.Fhir.Model.Integer(10)),
                ElementNode.ForPrimitive(91),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [int]"
            };
            yield return new object?[]
            {
                new PatternValidator(new Hl7.Fhir.Model.Integer(90)),
                ElementNode.ForPrimitive(90),
                true, null, "result must be true [int]"
            };
            // string
            yield return new object?[]
            {
                new PatternValidator(new FhirString("test")),
                ElementNode.ForPrimitive("testfailure"),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [string]"
            };
            yield return new object?[]
            {
                new PatternValidator(new  FhirString("test")),
                ElementNode.ForPrimitive("test"),
                true, null,"result must be true [string]"
            };
            // boolean
            yield return new object?[]
            {
                new PatternValidator(new FhirBoolean(true)),
                ElementNode.ForPrimitive(false),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [boolean]"
            };
            yield return new object?[]
            {
                new PatternValidator(new FhirBoolean(true)),
                ElementNode.ForPrimitive(true),
                true, null, "result must be true [boolean]"
            };
            // mixed primitive types
            yield return new object?[]
            {
                new PatternValidator( new Hl7.Fhir.Model.Date("2019-09-05")),
                ElementNode.ForPrimitive(20190905),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [mixed]"
            };
            // Complex types
            yield return new object?[]
            {
                new PatternValidator(new HumanName() { Family = "Brown", Given = ["Joe" ]} ),
                ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Joe", "Patrick" } ),
                true, null, "The input should match the pattern: family name should be Brown, and given name is Joe"
            };
            yield return new object?[]
            {
                new PatternValidator(new HumanName() { Family = "Brown", Given = ["Joe" ]}),
                ElementNode.ForPrimitive("Brown, Joe Patrick"),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "String and HumanName are different"
            };
            yield return new object?[]
            {
                new PatternValidator(new HumanName() { Family = "Brown", Given = ["Joe" ]}),
                ElementNodeAdapterExtensions.CreateHumanName("Brown", Array.Empty<string>() ),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "The input should not match the pattern"
            };
        }
    }

    [TestClass]
    public class PatternValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void InvalidConstructors()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _ = new PatternValidator((DataType?)null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new PatternValidator(new Hl7.Fhir.Model.Integer(4));

            assertion.Should().NotBeNull();
            assertion.PatternValue.Should().BeAssignableTo<DataType>();
        }

        [DataTestMethod]
        [PatternValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}