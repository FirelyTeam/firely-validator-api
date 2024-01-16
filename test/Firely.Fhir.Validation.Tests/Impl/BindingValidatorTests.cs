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
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using static Firely.Fhir.Validation.ValidationSettings;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingValidatorTests
    {
        private readonly BindingValidator _bindingAssertion;
        private readonly ValidationSettings _validationSettingsM;
        private readonly Mock<ICodeValidationTerminologyService> _validateCodeService;

        private static readonly string CONTEXT = "some.uri#path";


        public BindingValidatorTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion = new BindingValidator(valueSetUri, BindingValidator.BindingStrength.Required, true, CONTEXT);

            _validateCodeService = new Mock<ICodeValidationTerminologyService>();
            _validationSettingsM = ValidationSettings.BuildMinimalContext(validateCodeService: _validateCodeService.Object);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException), "No input is present")]
        public void NoInputPresent()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = _bindingAssertion.Validate(null, _validationSettingsM, new ValidationState());
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [TestMethod()]
        public void ValidateTest()
        {
            var input = ElementNode.ForPrimitive(true);
            _ = _bindingAssertion.Validate(input, _validationSettingsM);
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
            if (text is not null)
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

        private void setup(bool success, string? message)
        {
            var result = new Parameters
                {
                    { "message", new FhirString(message) },
                    { "result", new FhirBoolean(success) }
                };

            _validateCodeService.Setup(vs => vs.ValueSetValidateCode(It.IsAny<Parameters>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(System.Threading.Tasks.Task.FromResult(result));
        }

        private void setup(Exception e)
        {
            _validateCodeService.Setup(vs => vs.ValueSetValidateCode(It.IsAny<Parameters>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(e);
        }

        private void verify(Predicate<ValidateCodeParameters> pred)
        {
            _validateCodeService.Verify(p =>
                p.ValueSetValidateCode(It.Is<Parameters>(p => pred(new ValidateCodeParameters(p))),
                    It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public void ValidateWithCode()
        {
            //_vcs.Setup(true, null);
            setup(true, null);
            var input = ElementNodeAdapter.Root("code", value: "CD123");

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(p => p.Code.IsExactly(new Code("CD123")));
        }

        [TestMethod]
        public void ValidateWithUri()
        {
            setup(true, null);
            var input = ElementNodeAdapter.Root("uri", value: "http://some.uri");

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(p => p.Code.IsExactly(new Code("http://some.uri")));
        }

        [TestMethod]
        public void ValidateWithString()
        {
            setup(true, null);
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(p => p.Code.IsExactly(new Code("Some string")));
        }

        [TestMethod]
        public void ValidateWithCoding()
        {
            setup(true, null);

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts => ts.Coding.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")));
        }

        [TestMethod]
        public void ValidateWithCodeableConcept()
        {
            setup(true, null);
            var codings = new[] { createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
            createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")};

            var input = createConcept(codings);

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts => ts.CodeableConcept is not null);
        }

        [TestMethod]
        public void ValidateWithQuantity()
        {
            setup(true, null);

            var input = createQuantity(25, "s");
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts => ts.Coding.IsExactly(new Coding("http://unitsofmeasure.org", "s")));
        }

        [TestMethod]
        public void ValidateEmptyString()
        {
            var input = ElementNodeAdapter.Root("string", value: "");

            _ = _bindingAssertion.Validate(input, _validationSettingsM);

            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateCodingWithoutCode()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var input = createCoding("system", null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateInvalidCoding()
        {
            setup(false, "Not found");

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsFalse(result.IsSuccessful);
            verify(ts => ts.Coding.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN")));
        }

        [TestMethod]
        public void ValidateWithUnreachableTerminologyServer()
        {
            setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCodeWithUnreachableTerminologyServerAndUserIntervention()
        {
            setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));
            var validationSettings = ValidationSettings.BuildMinimalContext(_validateCodeService.Object);
            validationSettings.HandleValidateCodeServiceFailure = userIntervention;

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, validationSettings);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();

            input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "ERROR");
            result = _bindingAssertion.Validate(input, validationSettings);

            result.Warnings.Should().BeEmpty();
            result.Errors.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_ERROR.Code);

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p, FhirOperationException e)
                => p.Coding.Code.StartsWith("UNKNOWN") ? TerminologyServiceExceptionResult.Warning : TerminologyServiceExceptionResult.Error;
        }

        [TestMethod]
        public void ValidateConceptWithUnreachableTerminologyServerAndUserIntervention()
        {
            setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var validationSettings = ValidationSettings.BuildMinimalContext(_validateCodeService.Object);
            validationSettings.HandleValidateCodeServiceFailure = userIntervention;

            var codings = new[] {
                createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
                createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "error")};
            var input = createConcept(codings);

            var result = _bindingAssertion.Validate(input, validationSettings);

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p, FhirOperationException e)
               => p.CodeableConcept.Coding.Last().Code.EndsWith("error") ? TerminologyServiceExceptionResult.Error : TerminologyServiceExceptionResult.Warning;
        }

        [TestMethod]
        public void ExceptionMessageTest()
        {
            setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var ValidationSettings = BuildMinimalContext(_validateCodeService.Object);

            var inputWithoutSystem = ElementNodeAdapter.Root("Coding");
            inputWithoutSystem.Add("code", "aCode", "string");

            var result = _bindingAssertion.Validate(inputWithoutSystem, ValidationSettings);
            result.Warnings.Should().OnlyContain(w => w.Message.StartsWith("Terminology service failed while validating coding 'aCode': Dummy message"));

            var inputWithSystem = createCoding("aSystem", "aCode");
            result = _bindingAssertion.Validate(inputWithSystem, ValidationSettings);
            result.Warnings.Should().OnlyContain(w => w.Message.StartsWith("Terminology service failed while validating coding 'aCode' (system 'aSystem'): Dummy message"));
        }
    }
}