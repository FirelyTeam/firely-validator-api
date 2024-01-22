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
    internal class FixedValidationData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            // integer
            yield return new object?[]
            {
                    new FixedValidator(ElementNode.ForPrimitive(10)),
                    ElementNode.ForPrimitive(91),
                    false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [int]"
            };
            yield return new object?[]
            {
                    new FixedValidator(ElementNode.ForPrimitive(90)),
                    ElementNode.ForPrimitive(90),
                    true, null, "result must be true [int]"
            };
            // string
            yield return new object?[]
            {
                     new FixedValidator(ElementNode.ForPrimitive("test")),
                     ElementNode.ForPrimitive("testfailure"),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [string]"
            };
            yield return new object?[]
            {
                     new FixedValidator(ElementNode.ForPrimitive("test")),
                     ElementNode.ForPrimitive("test"),
                     true, null,"result must be true [string]"
            };
            // boolean
            yield return new object?[]
            {
                     new FixedValidator(ElementNode.ForPrimitive(true)),
                     ElementNode.ForPrimitive(false),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [boolean]"
            };
            yield return new object?[]
            {
                     new FixedValidator(ElementNode.ForPrimitive(true)),
                     ElementNode.ForPrimitive(true),
                     true, null, "result must be true [boolean]"
            };
            // mixed primitive types
            yield return new object[]
            {
                     new FixedValidator(ElementNode.ForPrimitive("20190905")),
                     ElementNode.ForPrimitive(20190905),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [mixed]"
            };
            // Complex Types
            yield return new object?[]
            {
                 new FixedValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Joe"] ) ),
                 ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Joe"] ),
                 true, null, "The input should match: family name should be Brown, and given name is Joe"
            };
            yield return new object?[]
            {
                 new FixedValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Joe"] )),
                 ElementNode.ForPrimitive("Brown, Joe Patrick"),
                 false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "String and HumanName are different"
            };
            yield return new object?[]
            {
                 new FixedValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Joe", "Patrick"])),
                 ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Patrick", "Joe"]),
                 false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "The input should not match the fixed"
            };
            yield return new object?[]
           {
                 new FixedValidator( ElementNodeAdapterExtensions.CreateCoding( "code", "system", true)),
                     ElementNodeAdapterExtensions.CreateCoding( "code", "system", false),
                     true, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "The input should match the fixed, also when the order is not the same"
           };
        }
    }

    [TestClass]
    public class FixedValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void InvalidConstructors()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _ = new FixedValidator(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();
        }

        [DataTestMethod]
        [FixedValidationData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}
