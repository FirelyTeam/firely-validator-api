
/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
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
    internal class BindingValidator : IValidatable
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
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState s)
        {
            if (input is null) throw Error.ArgumentNull(nameof(input));
            if (input.InstanceType is null) throw Error.Argument(nameof(input), "Binding validation requires input to have an instance type.");
            if (vc.ValidateCodeService is null)
                throw new InvalidOperationException($"Encountered a ValidationSettings that does not have" +
                    $"its non-null {nameof(ValidationSettings.ValidateCodeService)} set.");

            // This would give informational messages even if the validation was run on a choice type with a binding, which is then
            // only applicable to an instance which is bindable. So instead of a warning, we should just return as validation is
            // not applicable to this instance.
            if (!ModelInspector.Base.IsBindable(input.InstanceType))
            {
                return vc.TraceResult(() =>
                    new TraceAssertion(s.Location.InstanceLocation.ToString(),
                        $"Validation of binding with non-bindable instance type '{input.InstanceType}' always succeeds."));
            }

            if (input.ParseBindable() is { } bindable)
            {
                var result = verifyContentRequirements(input, bindable, s);

                return result.IsSuccessful ?
                    validateCode(input, bindable, vc, s)
                    : result;
            }
            else
            {
                return new IssueAssertion(
                        Strength == BindingStrength.Required ?
                            Issue.CONTENT_INVALID_FOR_REQUIRED_BINDING :
                            Issue.CONTENT_INVALID_FOR_NON_REQUIRED_BINDING,
                            $"Type '{input.InstanceType}' is bindable, but could not be parsed.").AsResult(s);
            }
        }

        /// <summary>
        /// Validates whether the instance has the minimum required coded content, depending on the binding.
        /// </summary>
        /// <remarks>Will throw an <c>InvalidOperationException</c> when the input is not of a bindeable type.</remarks>
        private ResultReport verifyContentRequirements(IScopedNode source, Element bindable, ValidationState s)
        {
            switch (bindable)
            {
                case Code code when string.IsNullOrEmpty(code.Value) && Strength == BindingStrength.Required:
                case Coding cd when string.IsNullOrEmpty(cd.Code) && Strength == BindingStrength.Required:
                case CodeableConcept cc when !codeableConceptHasCode(cc) && Strength == BindingStrength.Required:
                    return new IssueAssertion(Issue.TERMINOLOGY_NO_CODE_IN_INSTANCE,
                        $"No code found in {source.InstanceType} with a required binding.").AsResult(s);
                case CodeableConcept cc when !codeableConceptHasCode(cc) && string.IsNullOrEmpty(cc.Text) &&
                                Strength == BindingStrength.Extensible:
                    return new IssueAssertion(Issue.TERMINOLOGY_NO_CODE_IN_INSTANCE,
                        $"Extensible binding requires code or text.").AsResult(s);
                default:
                    return ResultReport.SUCCESS;      // nothing wrong then
            }

            // Can't end up here
        }

        private static bool codeableConceptHasCode(CodeableConcept cc) =>
            cc.Coding.Any(cd => !string.IsNullOrEmpty(cd.Code));


        private ResultReport validateCode(IScopedNode source, Element bindable, ValidationSettings vc, ValidationState s)
        {
            //EK 20170605 - disabled inclusion of warnings/errors for all but required bindings since this will 
            // 1) create superfluous messages (both saying the code is not valid) coming from the validateResult + the outcome.AddIssue() 
            // 2) add the validateResult as warnings for preferred bindings, which are confusing in the case where the slicing entry is 
            //    validating the binding against the core and slices will refine it: if it does not generate warnings against the slice, 
            //    it should not generate warnings against the slicing entry.
            if (Strength != BindingStrength.Required) return ResultReport.SUCCESS;

            var parameters = buildParams()
                .WithValueSet(ValueSetUri.ToString())
                .WithAbstract(AbstractAllowed);

            ValidateCodeParameters buildParams()
            {
                var parameters = new ValidateCodeParameters();

                return bindable switch
                {
                    FhirString str => parameters.WithCode(str.Value, system: null, display: null, context: Context),
                    FhirUri uri => parameters.WithCode(uri.Value, system: null, display: null, context: Context),
                    Code co => parameters.WithCode(co.Value, system: null, display: null, context: Context),
                    Coding cd => parameters.WithCoding(cd),
                    CodeableConcept cc => parameters.WithCodeableConcept(cc),
                    _ => throw Error.InvalidOperation($"Parsed bindable was of unexpected instance type '{bindable.TypeName}'.")
                };
            }

            var display = buildCodingDisplay(parameters);
            var result = callService(parameters, vc, display);

            return result switch
            {
                (null, _) => ResultReport.SUCCESS,
                (Issue issue, var message) => new IssueAssertion(issue, message!).AsResult(s)
            };
        }

        private static string buildCodingDisplay(ValidateCodeParameters p)
        {
            return p switch
            {
                { Code: not null } code => "code " + codeToString(p.Code.Value, p.System?.Value),
                { Coding: { } coding } => "coding " + codeToString(coding.Code, coding.System),
                { CodeableConcept: { } cc } when !string.IsNullOrEmpty(cc.Text) => $"concept {cc.Text} with coding(s) {ccToString(cc)}",
                { CodeableConcept: { } cc } when string.IsNullOrEmpty(cc.Text) => $"concept with coding(s) {ccToString(cc)}",
                _ => throw new NotSupportedException("Logic error: one of code/coding/cc should have been not null.")
            };

            static string codeToString(string code, string? system)
            {
                var systemAddition = system is null ? string.Empty : $" (system '{system}')";
                return $"'{code}'{systemAddition}";
            }

            static string ccToString(CodeableConcept cc) =>
                string.Join(',', cc.Coding?.Select(c => codeToString(c.Code, c.System)) ?? Enumerable.Empty<string>());
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

        private static (Issue?, string?) interpretResults(Parameters parameters, string display)
        {
            var result = parameters.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = parameters.GetSingleValue<FhirString>("message")?.Value;

            return (result, message) switch
            {
                (true, null) => (null, null),
                (true, string) => (Issue.TERMINOLOGY_OUTPUT_WARNING, message),
                (false, null) => (Issue.TERMINOLOGY_OUTPUT_ERROR, display.Capitalize() + " is invalid, but the terminology service provided no further details."),
                (false, string) => (Issue.TERMINOLOGY_OUTPUT_ERROR, message)
            };
        }

        private static (Issue?, string?) callService(ValidateCodeParameters parameters, ValidationSettings ctx, string display)
        {
            try
            {
                var callParams = parameters.Build();
                return interpretResults(TaskHelper.Await(() => ctx.ValidateCodeService.ValueSetValidateCode(callParams)), display);
            }
            catch (FhirOperationException tse)
            {
                var desiredResult = ctx.HandleValidateCodeServiceFailure?.Invoke(parameters, tse)
                    ?? TerminologyServiceExceptionResult.Warning;

                var message = $"Terminology service failed while validating {display}: {tse.Message}";
                return desiredResult switch
                {
                    TerminologyServiceExceptionResult.Error => (Issue.TERMINOLOGY_OUTPUT_ERROR, message),
                    TerminologyServiceExceptionResult.Warning => (Issue.TERMINOLOGY_OUTPUT_WARNING, message),
                    _ => throw new NotSupportedException("Logic error: unknown terminology service exception result.")
                };
            }
        }
    }
}