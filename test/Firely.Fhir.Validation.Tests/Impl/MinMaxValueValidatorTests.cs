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
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    internal class MinValueValidatorData : BasicValidatorDataAttribute
    {
        private readonly IValidatable _validatableMinValue =
            new MinMaxValueValidator(PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(4), MinMaxValueValidator.ValidationMode.MinValue);
        private readonly IValidatable _validatableMaxValue =
            new MinMaxValueValidator(PrimitiveTypeExtensions.ToTypedElement<Date, string>("1905-08-23"), MinMaxValueValidator.ValidationMode.MaxValue);

        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToTypedElement<FhirString, string>("a string"),
                true, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, "CompareWithOtherPrimitive"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(3),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL, "LessThan"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(4),
                true, null, "Equals"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(5),
                true, null, "GreatThan"
            };

            yield return new object[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(2),
                true, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, "CompareWithOtherPrimitive"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Date, string>("1905-01-01"),
                true, null, "LessThan"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Date, string>("1905"),
                true, null, "PartialEquals"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Date, string>("1905-08-23"),
                true, null, "Equals"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Date, string>("1905-12-31"),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE, "GreaterThan"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToTypedElement<Date, string>("1906"),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE, "PartialGreaterThan"
            };
        }
    }

    [TestClass]
    public class MinMaxValueValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void InvalidConstructors()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => { _ = new MinMaxValueValidator(null, MinMaxValueValidator.ValidationMode.MaxValue); };
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().Throw<ArgumentNullException>();

            var humanNameValue = ElementNodeAdapter.Root("HumanName");
            humanNameValue.Add("family", "Brown", "string");

            action = () => _ = new MinMaxValueValidator(humanNameValue, MinMaxValueValidator.ValidationMode.MaxValue);
            action.Should().Throw<IncorrectElementDefinitionException>();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new MinMaxValueValidator(
                PrimitiveTypeExtensions.ToTypedElement<Integer, int?>(4),
                MinMaxValueValidator.ValidationMode.MaxValue);

            assertion.Should().NotBeNull();
            assertion.Limit.Should().BeAssignableTo<ITypedElement>();
        }

        [DataTestMethod]
        [MinValueValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}