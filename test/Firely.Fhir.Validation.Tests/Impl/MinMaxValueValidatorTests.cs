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
using Date = Hl7.Fhir.Model.Date;
using Integer = Hl7.Fhir.Model.Integer;

namespace Firely.Fhir.Validation.Tests
{
    internal class MinValueValidatorData : BasicValidatorDataAttribute
    {
        private readonly IValidatable _validatableMinValue =
            new MinMaxValueValidator(PrimitiveTypeExtensions.ToPocoNode<Integer>(4), MinMaxValueValidator.ValidationMode.MinValue);
        private readonly IValidatable _validatableMaxValue =
            new MinMaxValueValidator(PrimitiveTypeExtensions.ToPocoNode<Date>("1905-08-23"), MinMaxValueValidator.ValidationMode.MaxValue);

        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToPocoNode<FhirString>("a string"),
                true, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, "CompareWithOtherPrimitive"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToPocoNode<Integer>(3),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL, "LessThan"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToPocoNode<Integer>(4),
                true, null, "Equals"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToPocoNode<Integer>(5),
                true, null, "GreatThan"
            };

            yield return new object[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Integer>(2),
                true, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, "CompareWithOtherPrimitive"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Date>("1905-01-01"),
                true, null, "LessThan"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Date>("1905"),
                true, null, "PartialEquals"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Date>("1905-08-23"),
                true, null, "Equals"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Date>("1905-12-31"),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE, "GreaterThan"
            };
            yield return new object?[]
            {
                _validatableMaxValue,
                PrimitiveTypeExtensions.ToPocoNode<Date>("1906"),
                false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE, "PartialGreaterThan"
            };
            yield return new object?[]
            {
                _validatableMinValue,
                PrimitiveTypeExtensions.ToPocoNode<Integer64>("4"),
                true, null, "Equals"
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

            action = () => _ = new MinMaxValueValidator(humanNameValue.ToPocoNode(), MinMaxValueValidator.ValidationMode.MaxValue);
            action.Should().Throw<IncorrectElementDefinitionException>();
        }

        [TestMethod]
        public void QuantityComparison()
        {
            var maxValue = new Hl7.Fhir.Model.Quantity
            {
                Value = 4,
                Unit = "kg",
                System = "http://unitsofmeasure.org",
                Code = "kg"
            }.ToPocoNode(ModelInfo.ModelInspector);

            var assertion = new MinMaxValueValidator(maxValue, MinMaxValueValidator.ValidationMode.MaxValue);

            assertion.Should().NotBeNull();
            assertion.Limit.Should().BeAssignableTo<PocoNode>();

            var quantityCorrect = new Quantity
            {
                Value = 3,
                Unit = "kg",
                System = "http://unitsofmeasure.org",
                Code = "kg"
            }.ToPocoNode(ModelInfo.ModelInspector);

            base.BasicValidatorTestcases(assertion, quantityCorrect, true, null, "");

            var quantityIncorrect = new Quantity
            {
                Value = 5,
                Unit = "kg",
                System = "http://unitsofmeasure.org",
                Code = "kg"
            }.ToPocoNode(ModelInfo.ModelInspector);
            base.BasicValidatorTestcases(assertion, quantityIncorrect, false, Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE, "");
        }

        [TestMethod]
        public void PrimitiveConstructorWorks()
        {
            var assertion = new MinMaxValueValidator(4, MinMaxValueValidator.ValidationMode.MaxValue);
            
            var result = assertion.ValidateOne(PocoNode.ForPrimitive<Integer>(55), new(), new());

            assertion.Should().NotBeNull();
            assertion.Limit.Should().BeAssignableTo<PocoNode>();
            result.IsSuccessful.Should().BeFalse();
        }

        [TestMethod]
        public void CorrectConstructor()
        {
            var assertion = new MinMaxValueValidator(
                PrimitiveTypeExtensions.ToPocoNode<Integer>(4),
                MinMaxValueValidator.ValidationMode.MaxValue);

            assertion.Should().NotBeNull();
            assertion.Limit.Should().BeAssignableTo<PocoNode>();
        }

        [TestMethod]
        public void MissingValuesAndWrongLiteralTypesAreNotCompared()
        {
            var maxInteger = new MinMaxValueValidator(PrimitiveTypeExtensions.ToPocoNode<Integer>(10), MinMaxValueValidator.ValidationMode.MaxValue);
            var maxInteger64 = new MinMaxValueValidator(PrimitiveTypeExtensions.ToPocoNode<Integer64>("9223372036854775807"), MinMaxValueValidator.ValidationMode.MaxValue);
            var minDate = new MinMaxValueValidator(PrimitiveTypeExtensions.ToPocoNode<Date>("2000-01-01"), MinMaxValueValidator.ValidationMode.MinValue);

            // PrimitiveLiteralTypeValidator reports a wrong literal type, and a primitive without a value is allowed when it has extensions.
            issues(maxInteger, PocoNode.ForPrimitive<Integer>("abc")).Should().BeEmpty();
            issues(maxInteger, PocoNode.ForPrimitive<Integer>("5")).Should().BeEmpty();
            issues(maxInteger, PocoNode.ForPrimitive<Integer>(true)).Should().BeEmpty();
            issues(maxInteger, PocoNode.ForPrimitive(new Integer())).Should().BeEmpty();
            issues(minDate, PocoNode.ForPrimitive<Date>(1990)).Should().BeEmpty();

            // Other invalid values keep their warning, since nothing else reports e.g. a date that doesn't exist.
            issues(minDate, PocoNode.ForPrimitive<Date>("2021-02-30")).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE.Code);
            issues(maxInteger64, PocoNode.ForPrimitive<Integer64>("99999999999999999999")).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE.Code);

            // Valid values are still compared.
            issues(maxInteger, PocoNode.ForPrimitive<Integer>(11)).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE.Code);
            issues(minDate, PocoNode.ForPrimitive<Date>("1999-12-31")).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL.Code);
        }

        [TestMethod]
        public void QuantityWithoutComparableValueDoesNotThrow()
        {
            var maxValue = new MinMaxValueValidator(new Quantity(5, "mg").ToPocoNode(ModelInfo.ModelInspector), MinMaxValueValidator.ValidationMode.MaxValue);

            // A quantity without a value is allowed when it has extensions, and PrimitiveLiteralTypeValidator reports a string value or a numeric comparator or code.
            issues(maxValue, new Quantity { Unit = "mg" }.ToPocoNode(ModelInfo.ModelInspector)).Should().BeEmpty();
            issues(maxValue, new Quantity { ValueElement = new FhirDecimal { JsonValue = "3" }, Unit = "mg" }.ToPocoNode(ModelInfo.ModelInspector)).Should().BeEmpty();
            issues(maxValue, new Quantity { Value = 3, CodeElement = new Code { JsonValue = 5 } }.ToPocoNode(ModelInfo.ModelInspector)).Should().BeEmpty();
            issues(maxValue, new Quantity(3, "mg") { ComparatorElement = new Code<Quantity.QuantityComparator> { JsonValue = 5 } }.ToPocoNode(ModelInfo.ModelInspector)).Should().BeEmpty();

            // A comparator makes the quantity incomparable.
            issues(maxValue, new Quantity(3, "mg") { Comparator = Quantity.QuantityComparator.LessThan }.ToPocoNode(ModelInfo.ModelInspector)).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE.Code);

            issues(maxValue, new Quantity(6, "mg").ToPocoNode(ModelInfo.ModelInspector)).Should().ContainSingle()
                .Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE.Code);
        }

        private static IEnumerable<IssueAssertion> issues(IAssertion validator, PocoNode input) =>
            validator.Validate(input, ValidationSettings.BuildMinimalContext()).Evidence.OfType<IssueAssertion>();

        [DataTestMethod]
        [MinValueValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, PocoNode input, bool expectedResult, Issue? expectedIssue, string failureMessage)
            => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}