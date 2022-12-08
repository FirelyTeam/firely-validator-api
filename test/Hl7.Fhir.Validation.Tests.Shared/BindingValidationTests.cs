using Firely.Fhir.Validation;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;
using Moq;
using System.Linq;
using Xunit;

namespace Hl7.Fhir.Validation.Tests
{
    [Trait("Category", "Validation")]
    public class BindingValidationTests : IClassFixture<ValidationFixture>
    {
        private readonly IAsyncResourceResolver _resolver;
        private readonly ITerminologyService _termService;

        public BindingValidationTests(ValidationFixture fixture)
        {
            _resolver = fixture.AsyncResolver;
            _termService = new LocalTerminologyService(_resolver);
        }

        [Fact]
        public void TestValueValidation()
        {
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                Strength = BindingStrength.Required,
                ValueSet = "http://hl7.org/fhir/ValueSet/data-absent-reason"
            };

            var validator = binding.ToValidatable("http://example.org/fhir/StructureDefitition/fhir#text.path");
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);
            // Non-bindeable things should succeed
            Element v = new FhirBoolean(true);
            var node = v.ToTypedElement();
            Assert.True(validator.Validate(node, vc).IsSuccessful);

            v = new Quantity(4.0m, "masked", "http://terminology.hl7.org/CodeSystem/data-absent-reason");  // nonsense, but hey UCUM is not provided with the spec
            node = v.ToTypedElement();
            Assert.True(validator.Validate(node, vc).IsSuccessful);

            v = new Quantity(4.0m, "maskedx", "http://terminology.hl7.org/CodeSystem/data-absent-reason");  // nonsense, but hey UCUM is not provided with the spec
            node = v.ToTypedElement();
            Assert.False(validator.Validate(node, vc).IsSuccessful);

            v = new Quantity(4.0m, "kg");  // sorry, UCUM is not provided with the spec - still validate against data-absent-reason
            node = v.ToTypedElement();
            Assert.False(validator.Validate(node, vc).IsSuccessful);

            v = new FhirString("masked");
            node = v.ToTypedElement();
            Assert.True(validator.Validate(node, vc).IsSuccessful);

            v = new FhirString("maskedx");
            node = v.ToTypedElement();
            Assert.False(validator.Validate(node, vc).IsSuccessful);

            var ic = new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "masked");
            var ext = new Extension { Value = ic };
            node = ext.ToTypedElement();
            Assert.True(validator.Validate(node, vc).IsSuccessful);

            ic.Code = "maskedx";
            node = ext.ToTypedElement();
            Assert.False(validator.Validate(node, vc).IsSuccessful);
        }


        [Fact]
        public void TestCodingValidation()
        {
            var dar = "http://terminology.hl7.org/CodeSystem/data-absent-reason";

            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = "http://hl7.org/fhir/ValueSet/data-absent-reason",
                Strength = BindingStrength.Required
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);

            var c = new Coding(dar, "not-a-number");
            var result = val.Validate(c.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);

            c.Code = "NaNX";
            result = val.Validate(c.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);

            c.Code = "not-a-number";
            c.Display = "Not a Number (NaN)";
            binding.Strength = BindingStrength.Required;
            result = val.Validate(c.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);

            c.Display = "Not a NumberX";
            result = val.Validate(c.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);        // local terminology service treats incorrect displays as warnings (GH#624)

            // But this won't, it's also a composition, but without expansion - the local term server won't help you here
            var binding2 = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = "http://hl7.org/fhir/ValueSet/substance-code",
                Strength = BindingStrength.Required
            };

            var val2 = binding2.ToValidatable();

            c = new Coding("http://snomed.info/sct", "160244002");
            result = val2.Validate(c.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void TestEmptyIllegalAndLegalCode()
        {
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = "http://hl7.org/fhir/ValueSet/data-absent-reason",
                Strength = BindingStrength.Preferred
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);

            var cc = new CodeableConcept();
            cc.Coding.Add(new Coding(null, null, "Just some display text"));

            // First, no code at all should be ok with a preferred binding
            var result = val.Validate(cc.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);

            // Now, switch to a required binding
            binding.Strength = BindingStrength.Required;
            val = binding.ToValidatable();

            // Then, with no code at all in a CC with a required binding
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);
            Assert.Contains("No code found in", result.Evidence.OfType<IssueAssertion>().Single().Message);

            // Now with no code + illegal code
            cc.Coding.Add(new Coding("urn:oid:1.2.3.4.5", "16", "Here's a code"));
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);
            Assert.Contains("None of the Codings in the CodeableConcept were valid for the binding", result.Evidence.OfType<IssueAssertion>().Single().Message);

            // Now, add a third valid code according to the binding.
            cc.Coding.Add(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "asked-unknown"));
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void TestCodeableConceptValidation()
        {
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = "http://hl7.org/fhir/ValueSet/data-absent-reason",
                Strength = BindingStrength.Required
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);

            var cc = new CodeableConcept();
            cc.Coding.Add(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "not-a-number"));
            cc.Coding.Add(new Coding("http://terminology.hl7.org/CodeSystem/data-absent-reason", "not-asked"));

            var result = val.Validate(cc.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);

            cc.Coding.First().Code = "NaNX";
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);

            cc.Coding.Skip(1).First().Code = "did-ask";
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);

            //EK 2017-07-6 No longer reports warnings when failing a preferred binding
            binding.Strength = BindingStrength.Preferred;
            var val2 = binding.ToValidatable();
            result = val2.Validate(cc.ToTypedElement(), vc);
            Assert.True(result.IsSuccessful);
            Assert.Equal(0, result.Warnings.Count);
        }

        [Fact]
        public void TestValidationErrorMessageForCodings()
        {
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = new Model.Canonical("http://hl7.org/fhir/ValueSet/address-type"),
                Strength = BindingStrength.Required
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);

            var cc = new CodeableConcept();
            cc.Coding.Add(new Coding("http://non-existing.code.system", "01.015"));

            var result = val.Validate(cc.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);
            Assert.True(result.Errors.Count == 1);
            Assert.StartsWith("Code '01.015' from system 'http://non-existing.code.system' does not exist in valueset 'http://hl7.org/fhir/ValueSet/address-type'", result.Errors.First().Message);

            cc.Coding.Add(new Coding("http://another-non-existing.code.system", "01.016"));
            result = val.Validate(cc.ToTypedElement(), vc);
            Assert.False(result.IsSuccessful);
            Assert.True(result.Errors.Count == 1);
            result.Errors.First().Message.Should().StartWith(
                $"None of the Codings in the CodeableConcept were valid for the binding. Details follow.{System.Environment.NewLine}" +
                $"Code '01.015' from system 'http://non-existing.code.system' does not exist in valueset 'http://hl7.org/fhir/ValueSet/address-type'{System.Environment.NewLine}" +
                 "Code '01.016' from system 'http://another-non-existing.code.system' does not exist in valueset 'http://hl7.org/fhir/ValueSet/address-type'");

        }

        //MS 2021-16-08: Tests issue https://github.com/FirelyTeam/firely-net-sdk/issues/1857
        [Fact]
        public void TestCurrenciesValidation()
        {
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = new Model.Canonical("http://hl7.org/fhir/ValueSet/currencies"),
                Strength = BindingStrength.Required
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: _termService);

            var v = new Money() { Value = 1, Currency = Money.Currencies.EUR };
            var node = v.ToTypedElement();
            Assert.True(val.Validate(node, vc).IsSuccessful);
        }

        [Fact]
        public void ErrorMessageAtExceptionInService()
        {
            Mock<ITerminologyService> service = new();
            service.Setup(s => s.ValueSetValidateCode(It.IsAny<Parameters>(), null, false))
                .Throws(new FhirOperationException("Error", System.Net.HttpStatusCode.InternalServerError));
            var binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                ValueSet = "http://hl7.org/fhir/ValueSet/data-absent-reason",
                Strength = BindingStrength.Required
            };

            var val = binding.ToValidatable();
            var vc = ValidationContext.BuildMinimalContext(validateCodeService: service.Object);

            var code = new Code("aValue");

            var result = val.Validate(code.ToTypedElement(), vc);

            result.GetIssues().Should()
                .OnlyContain(i => i.Message.StartsWith("Terminology service failed while validating code 'aValue': Error"));

            var coding = new Coding("aSystem", "aValue");
            result = val.Validate(coding.ToTypedElement(), vc);

            result.GetIssues().Should()
                .OnlyContain(i => i.Message.StartsWith("Terminology service failed while validating coding 'aValue' (system 'aSystem'): Error"));

        }
    }
}
