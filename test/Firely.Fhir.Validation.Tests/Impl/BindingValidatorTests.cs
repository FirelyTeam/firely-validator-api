/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Firely.Fhir.Validation.ValidationContext;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingValidatorTests
    {
        private class MockedService : ICodeValidationTerminologyService
        {
            public Parameters Result { get; private set; } = new();
            public Exception? ExceptionResult { get; private set; } = null;

            public int Count;

            private int _lastCount;

            public Parameters? Last;

            public void Setup(bool success, string? message)
            {
                Result = new Parameters
                {
                    { "message", new FhirString(message) },
                    { "result", new FhirBoolean(success) }
                };

                _lastCount = Count = 0;
                Last = null;
            }

            public void Setup(Exception e)
            {
                ExceptionResult = e;
                Result = new();
                _lastCount = Count = 0;
                Last = null;
            }

            public Task<Parameters> Subsumes(Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();

            public Task<Parameters> ValueSetValidateCode(Parameters parameters, string? id = null, bool useGet = false)
            {
                Count += 1;

                if (ExceptionResult is not null) throw ExceptionResult;

                Last = parameters;
                return System.Threading.Tasks.Task.FromResult(Result);
            }

            public MockedService Once()
            {
                if (Count != 1) Assert.Fail($"Method was called {Count} times, not once.");

                return this;
            }
            public MockedService Verify(Predicate<ValidateCodeParameters> p)
            {
                _lastCount = Count;
                if (Last is null || !p(new ValidateCodeParameters(Last))) Assert.Fail("Verification failed.");
                return this;
            }


            public MockedService VerifyNoOtherCalls()
            {
                if (_lastCount != Count) Assert.Fail("Expected no other calls.");

                return this;
            }
        }

        private readonly BindingValidator _bindingAssertion;
        private readonly ValidationContext _validationContext;
        private readonly MockedService _vcs;

        private static readonly string CONTEXT = "some.uri#path";


        public BindingValidatorTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion = new BindingValidator(valueSetUri, BindingValidator.BindingStrength.Required, true, CONTEXT);

            _vcs = new MockedService();

            _validationContext = BuildMinimalContext(_vcs);
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
            _vcs.Setup(true, null);
            var input = ElementNodeAdapter.Root("code", value: "CD123");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(p => p.Code.IsExactly(new Code("CD123"))).Once();
        }

        [TestMethod]
        public void ValidateWithUri()
        {
            _vcs.Setup(true, null);
            var input = ElementNodeAdapter.Root("uri", value: "http://some.uri");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(p => p.Code.IsExactly(new Code("http://some.uri"))).Once();
        }

        [TestMethod]
        public void ValidateWithString()
        {
            _vcs.Setup(true, null);
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(p => p.Code.IsExactly(new Code("Some string"))).Once();
        }

        [TestMethod]
        public void ValidateWithCoding()
        {
            _vcs.Setup(true, null);

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(ts => ts.Coding.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked"))).Once();
        }

        [TestMethod]
        public void ValidateWithCodeableConcept()
        {
            _vcs.Setup(true, null);
            var codings = new[] { createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
            createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")};

            var input = createConcept(codings);

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(ts => ts.CodeableConcept is not null).Once();
        }

        [TestMethod]
        public void ValidateWithQuantity()
        {
            _vcs.Setup(true, null);

            var input = createQuantity(25, "s");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsTrue(result.IsSuccessful);
            _vcs.Verify(ts => ts.Coding.IsExactly(new Coding("http://unitsofmeasure.org", "s"))).Once();
        }

        [TestMethod]
        public void ValidateEmptyString()
        {
            var input = ElementNodeAdapter.Root("string", value: "");

            var result = _bindingAssertion.Validate(input, _validationContext);

            _vcs.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateCodingWithoutCode()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var input = createCoding("system", null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsFalse(result.IsSuccessful);
            _vcs.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ValidateInvalidCoding()
        {
            _vcs.Setup(false, "Not found");

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, _validationContext);

            Assert.IsFalse(result.IsSuccessful);
            _vcs.Verify(ts => ts.Coding.IsExactly(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN"))).Once();
        }

        [TestMethod]
        public void ValidateWithUnreachableTerminologyServer()
        {
            _vcs.Setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, _validationContext);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCodeWithUnreachableTerminologyServerAndUserIntervention()
        {
            _vcs.Setup(new FhirOperationException("Dummy", System.Net.HttpStatusCode.NotFound));
            var validationContext = ValidationContext.BuildMinimalContext(validateCodeService: _vcs);
            validationContext.OnValidateCodeServiceFailure = userIntervention;

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = _bindingAssertion.Validate(input, validationContext);

            result.Warnings.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_WARNING.Code);
            result.Errors.Should().BeEmpty();

            input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "ERROR");
            result = _bindingAssertion.Validate(input, validationContext);

            result.Warnings.Should().BeEmpty();
            result.Errors.Should().OnlyContain(w => w.IssueNumber == Issue.TERMINOLOGY_OUTPUT_ERROR.Code);

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p, FhirOperationException e)
                => p.Coding.Code.StartsWith("UNKNOWN") ? TerminologyServiceExceptionResult.Warning : TerminologyServiceExceptionResult.Error;
        }

        [TestMethod]
        public void ValidateConceptWithUnreachableTerminologyServerAndUserIntervention()
        {
            _vcs.Setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var validationContext = ValidationContext.BuildMinimalContext(_vcs);
            validationContext.OnValidateCodeServiceFailure = userIntervention;

            var codings = new[] {
                createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
                createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "error")};
            var input = createConcept(codings);

            var result = _bindingAssertion.Validate(input, validationContext);

            static TerminologyServiceExceptionResult userIntervention(ValidateCodeParameters p, FhirOperationException e)
               => p.CodeableConcept.Coding.Last().Code.EndsWith("error") ? TerminologyServiceExceptionResult.Error : TerminologyServiceExceptionResult.Warning;
        }

        [TestMethod]
        public void ExceptionMessageTest()
        {
            _vcs.Setup(new FhirOperationException("Dummy message", System.Net.HttpStatusCode.NotFound));
            var validationContext = BuildMinimalContext(validateCodeService: _vcs);

            var inputWithoutSystem = ElementNodeAdapter.Root("Coding");
            inputWithoutSystem.Add("code", "aCode", "string");

            var result = _bindingAssertion.Validate(inputWithoutSystem, validationContext);
            result.Warnings.Should().OnlyContain(w => w.Message.StartsWith("Terminology service failed while validating coding 'aCode': Dummy message"));

            var inputWithSystem = createCoding("aSystem", "aCode");
            result = _bindingAssertion.Validate(inputWithSystem, validationContext);
            result.Warnings.Should().OnlyContain(w => w.Message.StartsWith("Terminology service failed while validating coding 'aCode' (system 'aSystem'): Dummy message"));
        }
    }
}