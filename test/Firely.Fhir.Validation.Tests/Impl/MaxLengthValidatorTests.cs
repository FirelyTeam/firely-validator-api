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
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    internal class MaxLengthValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                PocoNode.ForPrimitive<FhirString>("12345678901"),
                false, Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "LengthTooLong"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                PocoNode.ForPrimitive<FhirString>("1234567890"),
                true, null, "Length correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                PocoNode.ForPrimitive<FhirString>("1"),
                true, null, "Length correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                PocoNode.ForPrimitive<FhirString>(""),
                true, null, "Empty string is correct"
            };
            yield return new object?[]
            {
                new MaxLengthValidator(10),
                PocoNode.ForPrimitive<FhirString>(90),
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
        public override void BasicValidatorTestcases(IAssertion assertion, PocoNode input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);

        [TestMethod]
        public void TooLongMessageShowsAtMost100CharactersOfTheValue()
        {
            var x99 = new string('x', 99);

            message(3, "abcd").Should().Be("Value 'abcd' is too long (maximum length is 3)");
            message(99, x99 + "x").Should().Be($"Value '{x99}x' is too long (maximum length is 99)");
            message(99, x99 + "xy").Should().Be($"Value '{x99}x...' is too long (101 characters, maximum length is 99)");
            message(1024 * 1024, new string('x', 1024 * 1024 + 1)).Should().Be($"Value '{x99}x...' is too long (1048577 characters, maximum length is 1048576)");

            // The cut doesn't split the surrogate pair of an emoji in half.
            message(99, x99 + "\U0001F600y").Should().Be($"Value '{x99}...' is too long (102 characters, maximum length is 99)");
            message(99, x99[1..] + "\U0001F600y").Should().Be($"Value '{x99[1..]}\U0001F600...' is too long (101 characters, maximum length is 99)");

            static string message(int maximumLength, string value) =>
                new MaxLengthValidator(maximumLength).Validate(PocoNode.ForPrimitive<FhirString>(value), ValidationSettings.BuildMinimalContext())
                    .Evidence.OfType<IssueAssertion>().Single().Message;
        }
    }
}
