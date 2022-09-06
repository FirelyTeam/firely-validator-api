/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath.Sprache;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion that expresses terminology binding requirements for a coded element.
    /// </summary>
    [DataContract]
    public class BindingValidator : IValidatable
    {
        /// <summary>
        /// How strongly use of the valueset specified in the binding is encouraged or enforced.
        /// </summary>
        public enum BindingStrength
        {
            /// <summary>
            /// To be conformant, instances of this element SHALL include a code from the specified value set.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("required", "http://hl7.org/fhir/binding-strength"), Description("Required")]
            Required,

            /// <summary>
            /// To be conformant, instances of this element SHALL include a code from the specified value set if any of the codes within the value set can apply to the concept being communicated.  If the valueset does not cover the concept (based on human review), alternate codings (or, data type allowing, text) may be included instead.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("extensible", "http://hl7.org/fhir/binding-strength"), Description("Extensible")]
            Extensible,

            /// <summary>
            /// Instances are encouraged to draw from the specified codes for interoperability purposes but are not required to do so to be considered conformant.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("preferred", "http://hl7.org/fhir/binding-strength"), Description("Preferred")]
            Preferred,

            /// <summary>
            /// Instances are not expected or even encouraged to draw from the specified value set.  The value set merely provides examples of the types of concepts intended to be included.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("example", "http://hl7.org/fhir/binding-strength"), Description("Example")]
            Example,
        }

        /// <summary>
        /// Uri for the valueset to validate the code in the instance against.
        /// </summary>
        [DataMember]
        public Canonical ValueSetUri { get; private set; }

        /// <summary>
        /// Binding strength for the binding - determines whether an incorrect code is an error.
        /// </summary>
        [DataMember]
        public BindingStrength? Strength { get; private set; }

        /// <summary>
        /// Whether abstract codes (that exist mostly for subsumption queries) may be used
        /// in an instance.
        /// </summary>
        [DataMember]
        public bool AbstractAllowed { get; private set; }

        /// <summary>
        /// The context of the value set, so that the server can resolve this to a value set to 
        /// validate against. 
        /// </summary>
        [DataMember]
        public string? Context { get; private set; }

        /// <summary>
        /// Constructs a validator for validating a coded element.
        /// </summary>
        /// <param name="valueSetUri">Value set Canonical URL</param>
        /// <param name="strength">Indicates the degree of conformance expectations associated with this binding</param>
        /// <param name="abstractAllowed"></param>
        /// <param name="context">The context of the value set, so that the server can resolve this to a value set to validate against.</param>
        public BindingValidator(Canonical valueSetUri, BindingStrength? strength, bool abstractAllowed = true, string? context = null)
        {
            ValueSetUri = valueSetUri;
            Strength = strength;
            AbstractAllowed = abstractAllowed;
            Context = context;
        }

        /// <inheritdoc />
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState _)
        {
            if (input is null) throw Error.ArgumentNull(nameof(input));
            if (input.InstanceType is null) throw Error.Argument(nameof(input), "Binding validation requires input to have an instance type.");
            if (vc.ValidateCodeService is null)
                throw new InvalidOperationException($"Encountered a ValidationContext that does not have" +
                    $"its non-null {nameof(ValidationContext.ValidateCodeService)} set.");

            // This would give informational messages even if the validation was run on a choice type with a binding, which is then
            // only applicable to an instance which is bindable. So instead of a warning, we should just return as validation is
            // not applicable to this instance.
            if (!isBindable(input.InstanceType))
            {
                return vc.TraceResult(() =>
                    new TraceAssertion(input.Location,
                        $"Validation of binding with non-bindable instance type '{input.InstanceType}' always succeeds."));
            }

            if (!tryParseBindable(input, out var bindable))
            {
                return new IssueAssertion(
                        Strength == BindingStrength.Required ?
                            Issue.CONTENT_INVALID_FOR_REQUIRED_BINDING :
                            Issue.CONTENT_INVALID_FOR_NON_REQUIRED_BINDING,
                            input.Location,
                            $"Type '{input.InstanceType}' is bindable, but could not be parsed.").AsResult();
            }

            var result = verifyContentRequirements(input, bindable);

            return result.IsSuccessful ?
                validateCode(input, bindable, vc)
                : result;
        }

        private static bool isBindable(string type) =>
            type switch
            {
                // This is the fixed list, for all FHIR versions
                "code" or "Coding" or "CodeableConcept" or "Quantity" or "string" or "uri" or "Extension" => true,
                _ => false,
            };

        private static bool tryParseBindable(ITypedElement input, out object bindable)
        {
            bindable = input.ParseBindable();
            return bindable is not null;
        }

        /// <summary>
        /// Validates whether the instance has the minimum required coded content, depending on the binding.
        /// </summary>
        /// <remarks>Will throw an <c>InvalidOperationException</c> when the input is not of a bindeable type.</remarks>
        private ResultReport verifyContentRequirements(ITypedElement source, object bindable)
        {
            switch (bindable)
            {
                // Note: parseBindable with translate all bindable types to just code/Coding/Concept,
                // so that's all we need to expect here.
                case string co when string.IsNullOrEmpty(co) && Strength == BindingStrength.Required:
                case Code code when string.IsNullOrEmpty(code.Value) && Strength == BindingStrength.Required:
                case Coding cd when string.IsNullOrEmpty(cd.Code) && Strength == BindingStrength.Required:
                case CodeableConcept cc when !codeableConceptHasCode(cc) && Strength == BindingStrength.Required:
                    return new IssueAssertion(Issue.TERMINOLOGY_NO_CODE_IN_INSTANCE, source.Location, $"No code found in {source.InstanceType} with a required binding.").AsResult();
                case CodeableConcept cc when !codeableConceptHasCode(cc) && string.IsNullOrEmpty(cc.Text) &&
                                Strength == BindingStrength.Extensible:
                    return new IssueAssertion(Issue.TERMINOLOGY_NO_CODE_IN_INSTANCE, source.Location, $"Extensible binding requires code or text.").AsResult();
                default:
                    return ResultReport.SUCCESS;      // nothing wrong then
            }

            // Can't end up here
        }

        private static bool codeableConceptHasCode(CodeableConcept cc) =>
            cc.Coding.Any(cd => !string.IsNullOrEmpty(cd.Code));

        private ResultReport validateCode(ITypedElement source, object bindable, ValidationContext vc)
        {
            //EK 20170605 - disabled inclusion of warnings/errors for all but required bindings since this will 
            // 1) create superfluous messages (both saying the code is not valid) coming from the validateResult + the outcome.AddIssue() 
            // 2) add the validateResult as warnings for preferred bindings, which are confusing in the case where the slicing entry is 
            //    validating the binding against the core and slices will refine it: if it does not generate warnings against the slice, 
            //    it should not generate warnings against the slicing entry.
            if (Strength != BindingStrength.Required) return ResultReport.SUCCESS;

            var service = new ValidateCodeServiceWrapper(vc.ValidateCodeService, vc.TerminologyServiceExceptionHandling);

            var vcsResult = bindable switch
            {
                string code => service.ValidateCode(ValueSetUri, new(system: null, code: code), AbstractAllowed, Context),
                Code code => service.ValidateCode(ValueSetUri, code.ToSystemCode(), AbstractAllowed, Context),
                Coding cd => service.ValidateCode(ValueSetUri, cd.ToSystemCode(), AbstractAllowed, Context),
                CodeableConcept cc => service.ValidateConcept(ValueSetUri, cc.ToSystemConcept(), AbstractAllowed, Context),
                _ => throw Error.InvalidOperation($"Parsed bindable was of unexpected instance type '{bindable.GetType().Name}'."),
            };

            return vcsResult.Message switch
            {
                not null => new IssueAssertion(vcsResult.Success ? Issue.TERMINOLOGY_OUTPUT_WARNING : Issue.TERMINOLOGY_OUTPUT_ERROR,
                        source.Location, vcsResult.Message).AsResult(),
                null when vcsResult.Success => ResultReport.SUCCESS,
                _ => new IssueAssertion(Issue.TERMINOLOGY_OUTPUT_ERROR, source.Location,
                        "Terminology service indicated failure, but returned no error message for explanation.").AsResult()
            };
        }

        /// <inheritdoc/>
        public JToken ToJson()
        {
            var props = new JObject(new JProperty("abstractAllowed", AbstractAllowed));
            if (Strength is not null)
                props.Add(new JProperty("strength", Strength!.GetLiteral()));
            if (ValueSetUri is not null)
                props.Add(new JProperty("valueSet", (string)ValueSetUri));

            return new JProperty("binding", props);
        }

        /// <summary>
        /// A wrapper around the IValidateCodeService to catch exceptions during method execution.
        /// </summary>
        private class ValidateCodeServiceWrapper : IValidateCodeService
        {
            private readonly IValidateCodeService _service;
            private readonly Func<Canonical, string, bool, string?, ValidationContext.TerminologyServiceExceptionResult> _tsExceptionHandling;

            public ValidateCodeServiceWrapper(IValidateCodeService service, Func<Canonical, string, bool, string?, ValidationContext.TerminologyServiceExceptionResult>? _tsExceptionHandling)
            {
                _service = service;
                this._tsExceptionHandling = _tsExceptionHandling ?? ((_, _, _, _) => ValidationContext.TerminologyServiceExceptionResult.Warning);
            }

            public CodeValidationResult ValidateCode(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Code code, bool abstractAllowed, string? context = null)
            {
                CodeValidationResult result;

                try
                {
                    result = _service.ValidateCode(valueSetUrl, code, abstractAllowed, context);
                }
                catch (Exception tse)
                {
                    var userResult = _tsExceptionHandling(valueSetUrl, code.ToString(), abstractAllowed, context);
                    result = new(userResult == ValidationContext.TerminologyServiceExceptionResult.Warning, $"Terminology service failed while validating code '{code.Value}' (system '{code.System}'): {tse.Message}");
                }

                return result;
            }

            public CodeValidationResult ValidateConcept(Canonical valueSetUrl, Hl7.Fhir.ElementModel.Types.Concept cc, bool abstractAllowed, string? context = null)
            {
                CodeValidationResult result;

                try
                {
                    result = _service.ValidateConcept(valueSetUrl, cc, abstractAllowed, context);
                }
                catch (Exception tse)
                {
                    var codings = string.Join(',', cc.Codes?.Select(c => $"{c.System}#{c.Value}") ?? Enumerable.Empty<string>());
                    var userResult = _tsExceptionHandling(valueSetUrl, codings, abstractAllowed, context);
                    // we would like this to end up as a warning, so set Success to true, and provide a message
                    result = new(userResult == ValidationContext.TerminologyServiceExceptionResult.Warning, $"Terminology service failed while validating concept {cc.Display} with codings '{codings}'): {tse.Message}");
                }

                return result;
            }
        }
    }
}