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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingValidatorTests
    {
        private readonly BindingValidator _bindingAssertion;
        private readonly ValidationContext _validationContext;
        private readonly Mock<IValidateCodeService> _validateCodeService;


        public BindingValidatorTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion = new BindingValidator(valueSetUri, BindingValidator.BindingStrength.Required);

            _validateCodeService = new Mock<IValidateCodeService>();

            _validationContext = ValidationContext.BuildMinimalContext(validateCodeService: _validateCodeService.Object);
        }

        private void setupTerminologyServiceResult(CodeValidationResult result)
        {
            _validateCodeService.Setup(vs => vs.ValidateCode(It.IsAny<Canonical>(), It.IsAny<Code>(), true, null)).Returns(Task.FromResult(result));
            _validateCodeService.Setup(vs => vs.ValidateConcept(It.IsAny<Canonical>(), It.IsAny<Concept>(), true)).Returns(Task.FromResult(result));
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException), "No input is present")]
        public async Task NoInputPresent()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = await _bindingAssertion.Validate(null, _validationContext, new ValidationState()).ConfigureAwait(false);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [TestMethod()]
        public async Task ValidateTest()
        {
            var input = ElementNode.ForPrimitive(true);
            _ = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);
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
        public async Task ValidateWithCode()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("code", value: "CD123");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(vs => vs.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                 new Code(null, "CD123", null, null), // code
                true,  // abstract
                null // context
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithUri()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("uri", value: "http://some.uri");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                new Code(null, "http://some.uri", null, null), // code
                true,  // abstract
                null // concept
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithString()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
                It.IsAny<Canonical>(), // valueSetUrl
                new Code(null, "Some string", null, null), // code
                true,  // abstract
                null // concept
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithCoding()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked", null, null), //code
               true,  // abstract
               null // concept
            ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithCodeableConcept()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));
            var codings = new[] { createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
            createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")};

            var input = createConcept(codings);

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateConcept(
                It.IsAny<Canonical>(), // valueSetUrl
                It.IsNotNull<Concept>(), //concept
                true  // abstract
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithQuantity()
        {
            setupTerminologyServiceResult(new CodeValidationResult(true, null));

            var input = createQuantity(25, "s");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://unitsofmeasure.org", "s", null, null), // code
               true,  // abstract
               null // concept
            ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateEmptyString()
        {
            var input = ElementNodeAdapter.Root("string", value: "");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ValidateCodingWithoutCode()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var input = createCoding("system", null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ValidateInvalidCoding()
        {
            setupTerminologyServiceResult(new CodeValidationResult(false, "Not found"));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.Verify(ts => ts.ValidateCode(
               It.IsAny<Canonical>(), // valueSetUrl
               new Code("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN", null, null), // code
               true,  // abstract
               null // concept
            ), Times.Once());
        }
    }
}