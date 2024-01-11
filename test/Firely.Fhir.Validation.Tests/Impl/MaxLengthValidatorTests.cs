/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    internal class MaxLengthValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive("12345678901"),
                false, Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "LengthTooLong"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive("1234567890"),
                true, null, "Length correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive("1"),
                true, null, "Length correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive(""),
                true, null, "Empty string is correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive(90),
                true, null, "MaxLength constraint on a non-string primitive should be a success"
            };
        }
    }

    [TestClass]
    public class MaxLengthValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void InvalidConstructors()
        {
            Action action = () => _ = new MaxLengthValidator(0);
            action.Should().Throw<IncorrectElementDefinitionException>();

            action = () => _ = new MaxLengthValidator(-9);
            action.Should().Throw<IncorrectElementDefinitionException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new MaxLengthValidator(4);

            assertion.Should().NotBeNull();
            assertion.ToJson().ToString().Should().Be("\"maxLength\": 4");
        }

        [DataTestMethod]
        [MaxLengthValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}
