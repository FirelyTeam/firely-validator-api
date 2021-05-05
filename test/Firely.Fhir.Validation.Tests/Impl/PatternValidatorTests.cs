/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class PatternValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            // integer
            yield return new object?[]
            {
                new PatternValidator(10),
                ElementNode.ForPrimitive(91),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [int]"
            };
            yield return new object?[]
            {
                new PatternValidator(90),
                ElementNode.ForPrimitive(90),
                true, null, "result must be true [int]"
            };
            // string
            yield return new object?[]
            {
                new PatternValidator("test"),
                ElementNode.ForPrimitive("testfailure"),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [string]"
            };
            yield return new object?[]
            {
                new PatternValidator("test"),
                ElementNode.ForPrimitive("test"),
                true, null,"result must be true [string]"
            };
            // boolean
            yield return new object?[]
            {
                new PatternValidator(true),
                ElementNode.ForPrimitive(false),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [boolean]"
            };
            yield return new object?[]
            {
                new PatternValidator(true),
                ElementNode.ForPrimitive(true),
                true, null, "result must be true [boolean]"
            };
            // mixed primitive types
            yield return new object?[]
            {
                new PatternValidator(Date.Parse("2019-09-05")),
                ElementNode.ForPrimitive(20190905),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "result must be false [mixed]"
            };
            // Complex types
            yield return new object?[]
            {
                new PatternValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Joe" } )),
                ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Joe", "Patrick" } ),
                true, null, "The input should match the pattern: family name should be Brown, and given name is Joe"
            };
            yield return new object?[]
            {
                new PatternValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Joe" } )),
                ElementNode.ForPrimitive("Brown, Joe Patrick"),
                false, Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, "String and HumanName are different"
            };
            yield return new object?[]
            {
                new PatternValidator(ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Joe" } )),
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
            Action action = () => _ = new PatternValidator(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new PatternValidator(4);

            assertion.Should().NotBeNull();
            assertion.PatternValue.Should().BeAssignableTo<ITypedElement>();
        }

        [DataTestMethod]
        [PatternValidatorData]
        public override Task BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}