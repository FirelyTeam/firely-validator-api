using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class BindingAssertionTests
    {
        private readonly BindingAssertion _bindingAssertion;
        private readonly ValidationContext _validationContext;
        private readonly Mock<ITerminologyServiceNEW> _terminologyService;


        public BindingAssertionTests()
        {
            var valueSetUri = "http://hl7.org/fhir/ValueSet/data-absent-reason";
            _bindingAssertion = new BindingAssertion(valueSetUri, BindingAssertion.BindingStrength.Required);

            _terminologyService = new Mock<ITerminologyServiceNEW>();

            _validationContext = ValidationContext.BuildMinimalContext(terminologyService: _terminologyService.Object);
        }

        private void setupTerminologyServiceResult(Assertions result)
        {
            _terminologyService.Setup(ts => ts.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Coding>(), It.IsAny<CodeableConcept>(), It.IsAny<FhirDateTime>(), true, It.IsAny<string>())).Returns(Task.FromResult(result));
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException), "No input is present")]
        public async Task NoInputPresent()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = await _bindingAssertion.Validate(null, _validationContext).ConfigureAwait(false);
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
            setupTerminologyServiceResult(Assertions.SUCCESS);
            var input = ElementNodeAdapter.Root("code", value: "CD123");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
                It.IsAny<string>(), // canonical
                It.IsAny<string>(), // context
                "CD123", // code
                null, // system
                null, // version
                null, // display
                null, // coding
                null, // concept
                null, // date
                true,  // abstracy
                null // displayLanguage
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithUri()
        {
            setupTerminologyServiceResult(Assertions.SUCCESS);
            var input = ElementNodeAdapter.Root("uri", value: "http://some.uri");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
                It.IsAny<string>(), // canonical
                It.IsAny<string>(), // context
                "http://some.uri", // code
                null, // system
                null, // version
                null, // display
                null, // coding
                null, // concept
                null, // date
                true,  // abstracy
                null // displayLanguage
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithString()
        {
            setupTerminologyServiceResult(Assertions.SUCCESS);
            var input = ElementNodeAdapter.Root("string", value: "Some string");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
                It.IsAny<string>(), // canonical
                It.IsAny<string>(), // context
                "Some string", // code
                null, // system
                null, // version
                null, // display
                null, // coding
                null, // concept
                null, // date
                true,  // abstracy
                null // displayLanguage
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithCoding()
        {
            setupTerminologyServiceResult(Assertions.SUCCESS);

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
               It.IsAny<string>(), // canonical
               It.IsAny<string>(), // context
               null, // code
               null, // system
               null, // version
               null, // display
               It.Is<Coding>(cd => cd.Code == "masked" && cd.System == "http://terminology.hl7.org/CodeSystem/data-absent-reason"), // coding
               null, // concept
               null, // date
               true,  // abstracy
               null // displayLanguage
            ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithCodeableConcept()
        {
            setupTerminologyServiceResult(Assertions.SUCCESS);
            var codings = new[] { createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked") ,
            createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked")};

            var input = createConcept(codings);

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
                It.IsAny<string>(), // canonical
                It.IsAny<string>(), // context
                null, // code
                null, // system
                null, // version
                null, // display
                null, // coding
                It.IsNotNull<CodeableConcept>(), // concept
                null, // date
                true,  // abstracy
                null // displayLanguage
             ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateWithQuantity()
        {
            setupTerminologyServiceResult(Assertions.SUCCESS);

            var input = createQuantity(25, "s");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsTrue(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
               It.IsAny<string>(), // canonical
               It.IsAny<string>(), // context
               null, // code
               null, // system
               null, // version
               null, // display
               It.Is<Coding>(cd => cd.Code == "s"), // coding
               null, // concept
               null, // date
               true,  // abstracy
               null // displayLanguage
            ), Times.Once());
        }

        [TestMethod]
        public async Task ValidateEmptyString()
        {
            var input = ElementNodeAdapter.Root("string", value: "");

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.Result.IsSuccessful);
            _terminologyService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ValidateCodingWithoutCode()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var input = createCoding("system", null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.Result.IsSuccessful);
            _terminologyService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ValidateInvalidCoding()
        {
            setupTerminologyServiceResult(Assertions.FAILURE);

            var input = createCoding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "UNKNOWN");
            var result = await _bindingAssertion.Validate(input, _validationContext).ConfigureAwait(false);

            Assert.IsFalse(result.Result.IsSuccessful);
            _terminologyService.Verify(ts => ts.ValidateCode(
               It.IsAny<string>(), // canonical
               It.IsAny<string>(), // context
               null, // code
               null, // system
               null, // version
               null, // display
               It.Is<Coding>(cd => cd.Code == "UNKNOWN" && cd.System == "http://terminology.hl7.org/CodeSystem/data-absent-reason"), // coding
               null, // concept
               null, // date
               true,  // abstracy
               null // displayLanguage
            ), Times.Once());
        }
    }
}