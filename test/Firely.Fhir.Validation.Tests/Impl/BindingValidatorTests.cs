/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingValidatorTests
    {
        private readonly BindingValidator _bindingAssertion;
        private readonly ValidationContext _validationContext;
        private readonly Mock<IValidateCodeService> _validateCodeService;

        private static readonly string CONTEXT = "some.uri#path";


        public BindingValidatorTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion = new BindingValidator(valueSetUri, BindingValidator.BindingStrength.Required, true, CONTEXT);

            _validateCodeService = new Mock<IValidateCodeService>();

            _validationContext = ValidationContext.BuildMinimalContext(validateCodeService: _validateCodeService.Object);
        }

        private void setupTerminologyServiceResult(CodeValidationResult result)
        {
            _validateCodeService.Setup(vs => vs.ValidateCode(It.IsAny<Canonical>(), It.IsAny<Code>(), true, CONTEXT)).Returns(result);
            _validateCodeService.Setup(vs => vs.ValidateConcept(It.IsAny<Canonical>(), It.IsAny<Concept>(), true, CONTEXT)).Returns(result);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException), "No input is present")]
        public void NoInputPresent()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = _bindingAssertion.Validate(null, _validationContext, new ValidationState());
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [TestMethod()]
        public void ValidateTest()
        {
            var input = ElementNode.ForPrimitive(true);
            _ = _bindingAssertion.Validate(input, _validationContext);
        }

        private static ITypedElement createCoding(string system, string code, string? display = null)
        {
            var codingValue = ElementNodeAdapter.Root("Coding");
            codingValue.Add("system", system, "uri");
            codingValue.Add("code", code, "string");
            if (display is not null)
                codingValue.Add("display", display, "string");

            return codingValue;
        }


        private static ITypedElement createConcept(ITypedElement[] coding, string? text = null)
        {
            var conceptValue = ElementNodeAdapter.Root("CodeableConcept");

            foreach (var item in coding)
            {
                conceptValue.Add(item, "coding");
            }
            if (text is object)
                conceptValue.Add("text", text, "string");
            return conceptValue;
        }

        private static ITypedElement createQuantity(decimal value, string unit)
        {
            var quantityValue = ElementNodeAdapter.Root("Quantity");
            quantityValue.Add("value", value);
            quantityValue.Add("code", unit);
            return quantityValue;
        }

        [TestMethod]
        public void ValidateWithCode()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("code", value: "CD123");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(vs => vs.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                 new Code(null, "CD123", null, null), // code
                true,  // abstract
                CONTEXT // context
             ), Times.Once());
        }

        [TestMethod]
        public void ValidateWithUri()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("uri", value: "http://some.uri");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                new Code(null, "http://some.uri", null, null), // code
                true,  // abstract
                CONTEXT // context
             ), Times.Once());
        }

        [TestMethod]
        public void ValidateWithString()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                new Code(null, "Some string", null, null), // code
                true,  // abstract
                CONTEXT // context
             ), Times.Once());
        }

        [TestMethod]
        public void ValidateWithCoding()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked", null, null), //code
               true,  // abstract
               CONTEXT // context
            ), Times.Once());
        }

        [TestMethod]
        public void ValidateWithCodeableConcept()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var codings = new[] { createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
            createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")};

            var input = createConcept(codings);

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateConcept(
                It.IsAny<Canonical>(), // valueSetUrl
                It.IsNotNull<Concept>(), //concept
                true,  // abstract
                CONTEXT // context
             ), Times.Once());
        }

        [TestMethod]
        public void ValidateWithQuantity()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));

            var input = createQuantity(25, "s");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://unitsofmeasure.org", "s", null, null), // code
               true,  // abstract
               CONTEXT // context
            ), Times.Once());
        }

        [TestMethod]
        public void ValidateEmptyString()
        {
            var input = ElementNodeAdapter.Root("string", value: "");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateCodingWithoutCode()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var input = createCoding("system", null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateInvalidCoding()
        {
            setupTerminologyServiceResult(new CodeValidationResult(false, "Not found"));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN", null, null), // code
               true,  // abstract
               CONTEXT // context
            ), Times.Once());
        }
    }
}