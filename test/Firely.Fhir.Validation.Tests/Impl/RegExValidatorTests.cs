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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    internal class RegExValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[] { new RegExValidator("[0-9]"), ElementNode.ForPrimitive(1), true, null, "result must be true '[0-9]'" };
            yield return new object?[] { new RegExValidator("[0-9]"), ElementNode.ForPrimitive("a"), false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "result must be false '[0-9]'" };

            yield return new object?[]
            {
                new RegExValidator(@"^((\+31)|(0031)|0)(\(0\)|)(\d{1,3})(\s|\-|)(\d{8}|\d{4}\s\d{4}|\d{2}\s\d{2}\s\d{2}\s\d{2})$"),
                ElementNode.ForPrimitive("+31(0)612345678"), true, null, "result must be true (Dutch phonenumber"
            };
        }
    }

    [TestClass]
    public class RegExValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void InvalidConstructors()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _ = new RegExValidator(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();

            action = () => _ = new RegExValidator("__2398@)ahdajdlka ad ***********INVALID REGEX");
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new RegExValidator("[0-9]");

            assertion.Should().NotBeNull("valid regex");
            assertion.ToJson().ToString().Should().Be("\"regex\": \"[0-9]\"");
        }

        [DataTestMethod]
        [RegExValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);

    }
}