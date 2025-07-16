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
using System.Collections.Generic;
using System.Linq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingValidatorTests
    {
        private readonly BindingValidator _bindingAssertion;
        private readonly ValidationSettings _validationSettingsM;
        private readonly Mock<ICodeValidationTerminologyService> _validateCodeService;

        public BindingValidatorTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion =
                new BindingValidator(valueSetUri, BindingValidator.BindingStrength.Required, true);

            _validateCodeService = new Mock<ICodeValidationTerminologyService>();
            _validationSettingsM =
                ValidationSettings.BuildMinimalContext(validateCodeService: _validateCodeService.Object);
        }

        [TestMethod()]
        public void ValidateTest()
        {
            var input = PocoNode.ForPrimitive<FhirBoolean>(true);
            _ = _bindingAssertion.Validate(input, _validationSettingsM);
        }

        private void setup(bool success, string? message)
        {
            var result = new Parameters();
            result.Add("message", new FhirString(message));
            result.Add("result", new FhirBoolean(success));

            _validateCodeService.Setup(vs =>
                    vs.ValueSetValidateCode(It.IsAny<Parameters>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(System.Threading.Tasks.Task.FromResult(result));
        }

        private void setup(Exception e)
        {
            _validateCodeService.Setup(vs =>
                    vs.ValueSetValidateCode(It.IsAny<Parameters>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(e);
        }

        private void verify(Predicate<ValidateCodeParameters> pred)
        {
            _validateCodeService.Verify(svc =>
                svc.ValueSetValidateCode(It.Is<Parameters>(p => pred(new ValidateCodeParameters(p))),
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
            verify(p => p.Code?.IsExactly(new Code("CD123")) is true);
            verify(p => p.InferSystem?.Value == true);
        }

        [TestMethod]
        public void ValidateWithUri()
        {
            setup(true, null);
            var input = new FhirUri("http://some.uri").ToPocoNode();

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(p => p.Code?.IsExactly(new Code("http://some.uri")) is true);
        }

        [TestMethod]
        public void ValidateWithString()
        {
            setup(true, null);
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(p => p.Code?.IsExactly(new Code("Some string")) is true);
        }

        [TestMethod]
        public void ValidateWithCoding()
        {
            setup(true, null);

            var input =
                new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked").ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts =>
                ts.Coding?.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")) is true);
        }

        [TestMethod]
        public void ValidateWithCodeableConcept()
        {
            setup(true, null);
            var input = new CodeableConcept("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")
                .ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts => ts.CodeableConcept is not null);
        }

        [TestMethod]
        public void ValidateWithQuantity()
        {
            setup(true, null);

            var input = new Quantity(25, "s").ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts => ts.Coding?.IsExactly(new Coding("http://unitsofmeasure.org", "s")) is true);
        }

        [TestMethod]
        public void ValidateWithCodeableReference()
        {
            setup(true, null);

            var inputCc = new CodeableConcept("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var inputRef = new ResourceReference("http://some.uri");
            var inputCr = new CodeableReference { Concept = inputCc, Reference = inputRef };
            var result = _bindingAssertion.Validate(inputCr.ToPocoNode(), _validationSettingsM);

            Assert.IsTrue(result.IsSuccessful);
            verify(ts =>
                ts.CodeableConcept?.Coding.Single()
                    .IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")) is true);
        }

        [TestMethod]
        public void ValidateEmptyString()
        {
            var input = new FhirString("").ToPocoNode();

            _ = _bindingAssertion.Validate(input, _validationSettingsM);

            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateCodingWithoutCode()
        {
            var input = new Coding().ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsFalse(result.IsSuccessful);
            _validateCodeService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateInvalidCoding()
        {
            setup(false, "Not found");

            var input =
                new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN").ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            Assert.IsFalse(result.IsSuccessful);
            verify(ts =>
                ts.Coding?.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN")) is true);
        }

        [TestMethod]
        public void ValidateWithUnreachableTerminologyServer()
        {
            setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));

            var input =
                new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN").ToPocoNode();
            var result = _bindingAssertion.Validate(input, _validationSettingsM);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCodeableReference()
        {
            setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));

            var input = new CodeableReference
            {
                Concept = new CodeableConcept("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")
            }.ToPocoNode();

            var result = _bindingAssertion.Validate(input, _validationSettingsM);
            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();

            input = new CodeableReference
            {
                Reference = new ResourceReference("http://some.uri")
            }.ToPocoNode();

            result = _bindingAssertion.Validate(input, _validationSettingsM);
            result.IsSuccessful.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateCodeWithUnreachableTerminologyServerAndUserIntervention()
        {
            setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));
            var validationSettings = ValidationSettings.BuildMinimalContext(_validateCodeService.Object);
            validationSettings.HandleValidateCodeServiceFailure = userIntervention;

            var input =
                new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN").ToPocoNode();
            var result = _bindingAssertion.Validate(input, validationSettings);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();

            input = new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "ERROR").ToPocoNode();
            result = _bindingAssertion.Validate(input, validationSettings);

            result.Warnings.Should().BeEmpty();
            result.Errors.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_ERROR.Code);

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p,
                FhirOperationException e)
                => p.Coding?.Code?.StartsWith("UNKNOWN") is true
                    ? TerminologyServiceExceptionResult.Warning
                    : TerminologyServiceExceptionResult.Error;
        }

        [TestMethod]
        public void ValidateConceptWithUnreachableTerminologyServerAndUserIntervention()
        {
            setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var validationSettings = ValidationSettings.BuildMinimalContext(_validateCodeService.Object);
            validationSettings.HandleValidateCodeServiceFailure = userIntervention;

            var codings = new List<Coding>
            {
                new("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked"),
                new("http://terminology.hl7.org/CodeSystem/data-absent-reason", "error")
            };
            var input = new CodeableConcept { Coding = codings }.ToPocoNode();

            var result = _bindingAssertion.Validate(input, validationSettings);
            Assert.IsFalse(result.IsSuccessful);
            return;

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p,
                FhirOperationException e)
                => p.CodeableConcept?.Coding.Last().Code.EndsWith("error") is true
                    ? TerminologyServiceExceptionResult.Error
                    : TerminologyServiceExceptionResult.Warning;
        }

        [TestMethod]
        public void ExceptionMessageTest()
        {
            setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var validationSettings = ValidationSettings.BuildMinimalContext(_validateCodeService.Object);

            var inputWithoutSystem = ElementNodeAdapter.Root("Coding");
            inputWithoutSystem.Add("code", "aCode", "string");

            var result = _bindingAssertion.Validate(inputWithoutSystem, validationSettings);
            result.Warnings.Should().OnlyContain(w =>
                w.Message.StartsWith("Terminology service failed while validating coding 'aCode': Dummy message"));

            var inputWithSystem = new Coding("aSystem", "aCode").ToPocoNode();
            result = _bindingAssertion.Validate(inputWithSystem, validationSettings);
            result.Warnings.Should().OnlyContain(w =>
                w.Message.StartsWith(
                    "Terminology service failed while validating coding 'aCode' (system 'aSystem'): Dummy message"));
        }
    }
}