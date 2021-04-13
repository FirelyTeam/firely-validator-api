/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class MaxLengthAssertionData : BasicValidatorDataAttribute
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
            // TODO debatable: is MaxLength for an integer valid? It is now Undecided.
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                ElementNode.ForPrimitive(90),
                false, null, "MaxLength constraint on a non-string primitive is undecided == not succesful"
            };
        }
    }

    [TestClass]
    public class MaxLengthTests : BasicValidatorTests
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
            assertion.Key.Should().Be("maxLength");
            assertion.Value.Should().Be(4);
        }

        [DataTestMethod]
        [MaxLengthAssertionData]
        public override Task BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}
