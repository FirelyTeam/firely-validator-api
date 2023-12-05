﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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
    internal class FixedValidationData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            // integer
            yield return new object?[]
            {
                    new FixedValidator(new Hl7.Fhir.Model.Integer(10)),
                    ElementNode.ForPrimitive(91),
                    false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [int]"
            };
            yield return new object?[]
            {
                    new FixedValidator(new Hl7.Fhir.Model.Integer(90)),
                    ElementNode.ForPrimitive(90),
                    true, null, "result must be true [int]"
            };
            // string
            yield return new object?[]
            {
                     new FixedValidator(new FhirString("test")),
                     ElementNode.ForPrimitive("testfailure"),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [string]"
            };
            yield return new object?[]
            {
                     new FixedValidator(new FhirString("test")),
                     ElementNode.ForPrimitive("test"),
                     true, null,"result must be true [string]"
            };
            // boolean
            yield return new object?[]
            {
                     new FixedValidator(new FhirBoolean(true)),
                     ElementNode.ForPrimitive(false),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [boolean]"
            };
            yield return new object?[]
            {
                     new FixedValidator(new FhirBoolean(true)),
                     ElementNode.ForPrimitive(true),
                     true, null, "result must be true [boolean]"
            };
            // mixed primitive types
            yield return new object[]
            {
                     new FixedValidator(new Hl7.Fhir.Model.Date("2019-09-05")),
                     ElementNode.ForPrimitive(20190905),
                     false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "result must be false [mixed]"
            };
            // Complex Types
            yield return new object?[]
            {
                 new FixedValidator(new HumanName() { Family = "Brown", Given = [ "Joe" ] } ),
                 ElementNodeAdapterExtensions.CreateHumanName("Brown", ["Joe"] ),
                 true, null, "The input should match: family name should be Brown, and given name is Joe"
            };
            yield return new object?[]
            {
                 new FixedValidator(new HumanName() { Family = "Brown", Given = [ "Joe" ] } ),
                 ElementNode.ForPrimitive("Brown, Joe Patrick"),
                 false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "String and HumanName are different"
            };
            yield return new object?[]
            {
                 new FixedValidator(new HumanName() { Family = "Brown", Given = [ "Joe" , "Patricks"] }),
                 ElementNodeAdapterExtensions.CreateHumanName("Brown", new[] { "Patrick", "Joe" } ),
                 false, Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, "The input should not match the fixed"
            };
            yield return new object?[]
           {
                 new FixedValidator( new Coding("system", "code")),
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
            Action action = () => _ = new FixedValidator((DataType?)null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new FixedValidator(new Hl7.Fhir.Model.Integer(4));

            assertion.Should().NotBeNull();
            assertion.FixedValue.Should().BeAssignableTo<DataType>();
        }

        [DataTestMethod]
        [FixedValidationData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}
