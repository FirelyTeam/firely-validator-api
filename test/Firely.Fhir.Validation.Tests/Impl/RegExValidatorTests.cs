/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            assertion.Key.Should().Be("regex");
            assertion.Value.Should().Be("[0-9]");
        }

        [DataTestMethod]
        [RegExValidatorData]
        public override async Task BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => await base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);

    }
}