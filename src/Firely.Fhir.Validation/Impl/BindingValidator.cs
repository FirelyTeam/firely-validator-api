/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The aspects of the coded content that a <see cref="BindingValidator"/> checks.
    /// </summary>
    [Flags]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public enum CodedContentChecks
    {
        /// <summary>
        /// Check nothing about the coded content.
        /// </summary>
        None = 0,

        /// <summary>
        /// Check that the concepts are valid: that the instance carries the coded content its
        /// binding strength requires, and that the code(s) are in the bound value set (via the
        /// terminology service). When this check is disabled, the coded content is not checked
        /// at all - no terminology call is made, which also disables the <see cref="Displays"/> check.
        /// </summary>
        Concepts = 1,

        /// <summary>
        /// Check that the displays accompanying the code(s) are valid for those codes. This check rides
        /// the same terminology call as <see cref="Concepts"/>: when disabled, the display texts are
        /// omitted from the call, so the terminology service cannot (and will not) validate them.
        /// </summary>
        Displays = 2,

        /// <summary>
        /// Check the status of the applicable value sets and code systems. NOTE: this check cannot be
        /// implemented with the current terminology service interface and is accepted only so that
        /// configurations mentioning it can round-trip.
        /// </summary>
        Status = 4,

        /// <summary>
        /// The default set of checks, matching the validator's standard behavior.
        /// </summary>
        Default = Concepts | Displays
    }

    /// <summary>
    /// An assertion that expresses terminology binding requirements for a coded element.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
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
            [EnumLiteral("required", "http://hl7.org/fhir/binding-strength"), Hl7.Fhir.Utility.Description("Required")]
            Required,

            /// <summary>
            /// To be conformant, instances of this element SHALL include a code from the specified value set if any of the codes within the value set can apply to the concept being communicated.  If the valueset does not cover the concept (based on human review), alternate codings (or, data type allowing, text) may be included instead.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("extensible", "http://hl7.org/fhir/binding-strength"), Hl7.Fhir.Utility.Description("Extensible")]
            Extensible,

            /// <summary>
            /// Instances are encouraged to draw from the specified codes for interoperability purposes but are not required to do so to be considered conformant.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("preferred", "http://hl7.org/fhir/binding-strength"), Hl7.Fhir.Utility.Description("Preferred")]
            Preferred,

            /// <summary>
            /// Instances are not expected or even encouraged to draw from the specified value set.  The value set merely provides examples of the types of concepts intended to be included.<br/>
            /// (system: http://hl7.org/fhir/binding-strength)
            /// </summary>
            [EnumLiteral("example", "http://hl7.org/fhir/binding-strength"), Hl7.Fhir.Utility.Description("Example")]
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
        /// The aspects of the coded content this validator checks. Defaults to
        /// <see cref="CodedContentChecks.Default"/>, the validator's standard behavior.
        /// </summary>
        [DataMember]
        public CodedContentChecks Checks { get; private set; }

        /// <summary>
        /// Constructs a validator for validating a coded element.
        /// </summary>
        /// <param name="valueSetUri">Value set Canonical URL</param>
        /// <param name="strength">Indicates the degree of conformance expectations associated with this binding</param>
        /// <param name="abstractAllowed"></param>
        public BindingValidator(Canonical valueSetUri, BindingStrength? strength, bool abstractAllowed = true)
            : this(valueSetUri, strength, abstractAllowed, CodedContentChecks.Default)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a validator for validating a coded element, checking only the given aspects
        /// of the coded content.
        /// </summary>
        /// <param name="valueSetUri">Value set Canonical URL</param>
        /// <param name="strength">Indicates the degree of conformance expectations associated with this binding</param>
        /// <param name="abstractAllowed">Whether abstract codes may be used in an instance</param>
        /// <param name="checks">The aspects of the coded content to check</param>
        public BindingValidator(Canonical valueSetUri, BindingStrength? strength, bool abstractAllowed, CodedContentChecks checks)
        {
            ValueSetUri = valueSetUri;
            Strength = strength;
            AbstractAllowed = abstractAllowed;
            Checks = checks;
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState s)
        {
            if (input is null) throw Error.ArgumentNull(nameof(input));
            if (vc.ValidateCodeService is null)
                throw new InvalidOperationException($"Encountered a ValidationSettings that does not have" +
                    $"its non-null {nameof(ValidationSettings.ValidateCodeService)} set.");

            // This would give informational messages even if the validation was run on a choice type with a binding, which is then
            // only applicable to an instance which is bindable. So instead of a warning, we should just return as validation is
            // not applicable to this instance.
            if (!ModelInspector.Base.IsBindable(input.Poco.TypeName))
            {
                return vc.TraceResult(() =>
                    new TraceAssertion(input.GetLocation(),
                        $"Validation of binding with non-bindable instance type '{input.Poco.TypeName}' always succeeds."));
            }

            // When concept validation is disabled, no coded content is checked at all - this also
            // saves the (expensive) call to the terminology service.
            if (!Checks.HasFlag(CodedContentChecks.Concepts))
            {
                return vc.TraceResult(() =>
                    new TraceAssertion(input.GetLocation(),
                        $"Validation of the coded content against valueset '{ValueSetUri}' is disabled for this binding."));
            }

            if (input.ParseBindable() is DataType bindable)
            {
                var result = verifyContentRequirements(input, bindable, s);

                return result.IsSuccessful ?
                    ResultReport.Combine([result, validateCode(bindable, vc, s, input)])
                    : result;
            }
            else
            {
                // When there is no bindable content, the binding is not applicable to the instance, and we will
                // just return a successful result.
                return ResultReport.SUCCESS;
            }
        }

        /// <summary>
        /// Validates whether the instance has the minimum required coded content, depending on the binding.
        /// </summary>
        /// <remarks>Will throw an <c>InvalidOperationException</c> when the input is not of a bindeable type.</remarks>
        private ResultReport verifyContentRequirements(PocoNode source, DataType bindable, ValidationState s)
        {
            switch (bindable)
            {
                case Code code when string.IsNullOrEmpty(code.Value) && Strength == BindingStrength.Required:
                case Coding cd when string.IsNullOrEmpty(cd.Code) && Strength == BindingStrength.Required:
                case CodeableConcept cc when !codeableConceptHasCode(cc) && Strength == BindingStrength.Required:
                    return new IssueAssertion(Issue.TERMINOLOGY_INCOMPLETE_CODE_ERROR,
                        $"No code found in {source.Poco.TypeName} with a required binding to valueset '{ValueSetUri}'.").AsResult(s, source, nameof(BindingValidator), this);
                case CodeableConcept cc when !codeableConceptHasCode(cc) && string.IsNullOrEmpty(cc.Text) &&
                                Strength == BindingStrength.Extensible:
                    return new IssueAssertion(Issue.TERMINOLOGY_INCOMPLETE_CODE_WARNING,
                        $"Extensible binding to valueset '{ValueSetUri}' requires code or text.").AsResult(s, source, nameof(BindingValidator), this);
                default:
                    return ResultReport.SUCCESS;      // nothing wrong then
            }

            // Can't end up here
        }

        private static bool codeableConceptHasCode(CodeableConcept cc) =>
            cc.Coding.Any(cd => !string.IsNullOrEmpty(cd.Code));

        /// <summary>
        /// Returns the coding as-is when display checking is enabled; otherwise a copy without
        /// its display (leaving the instance untouched), so the terminology call has nothing to
        /// check the display against (per the $validate-code operation: "If no display is
        /// provided, the server cannot validate the display value").
        /// </summary>
        private Coding withDisplayCheck(Coding cd)
        {
            if (Checks.HasFlag(CodedContentChecks.Displays)) return cd;

            var stripped = (Coding)cd.DeepCopy();
            stripped.DisplayElement = null;
            return stripped;
        }

        /// <summary>
        /// Returns the concept as-is when display checking is enabled; otherwise a copy without
        /// the displays on its codings (leaving the instance untouched). See <see cref="withDisplayCheck(Coding)"/>.
        /// </summary>
        private CodeableConcept withDisplayCheck(CodeableConcept cc)
        {
            if (Checks.HasFlag(CodedContentChecks.Displays)) return cc;

            var stripped = (CodeableConcept)cc.DeepCopy();
            foreach (var coding in stripped.Coding)
                coding.DisplayElement = null;
            return stripped;
        }


        private ResultReport validateCode(Element bindable, ValidationSettings vc, ValidationState s, PocoNode input)
        {
            //EK 20170605 - disabled inclusion of warnings/errors for all but required bindings since this will 
            // 1) create superfluous messages (both saying the code is not valid) coming from the validateResult + the outcome.AddIssue() 
            // 2) add the validateResult as warnings for preferred bindings, which are confusing in the case where the slicing entry is 
            //    validating the binding against the core and slices will refine it: if it does not generate warnings against the slice, 
            //    it should not generate warnings against the slicing entry.
            if (Strength != BindingStrength.Required) return ResultReport.SUCCESS;

            var parameters = buildParams()
                .WithValueSet(new Hl7.Fhir.Model.Canonical(ValueSetUri.ToString())) //This should be cleaned up once we have one common Canonical type. 
                .WithAbstract(AbstractAllowed);

            ValidateCodeParameters buildParams()
            {
                var vcp = new ValidateCodeParameters();

                return bindable switch
                {
                    FhirString str => vcp.WithCode(str.Value, system: null, display: null, systemVersion: null, displayLanguage: null, context: null, inferSystem: false),
                    FhirUri uri => vcp.WithCode(uri.Value, system: null, display: null, systemVersion: null, displayLanguage: null, context: null, inferSystem: false),
                    Code co => vcp.WithCode(co.Value, system: null, display: null, systemVersion: null, displayLanguage: null, context: null, inferSystem: true),
                    Coding cd => vcp.WithCoding(withDisplayCheck(cd)),
                    CodeableConcept cc => vcp.WithCodeableConcept(withDisplayCheck(cc)),
                    _ => throw Error.InvalidOperation($"Parsed bindable was of unexpected instance type '{bindable.TypeName}'.")
                };
            }

            var display = buildCodingDisplay(parameters);
            var result = callService(parameters, vc, display);

            return result switch
            {
                (null, _) => ResultReport.SUCCESS,
                ({ } issue, var message) => new IssueAssertion(issue, (issue.Severity == OperationOutcome.IssueSeverity.Error ? message! + ", but the binding is of strength 'required'" : message!))
                    .AsResult(s, input, nameof(BindingValidator), this)
            };
        }

        private static string buildCodingDisplay(ValidateCodeParameters p)
        {
            return p switch
            {
                { Code.Value: { } code } => "code " + codeToString(code, p.System?.Value),
                { Coding.Code: { } code } => "coding " + codeToString(code, p.Coding.System),
                { CodeableConcept.Text: { Length: > 0 } text } => $"concept {text} with coding(s) {ccToString(p.CodeableConcept)}",
                { CodeableConcept: { } cc } when string.IsNullOrEmpty(cc.Text) => $"concept with coding(s) {ccToString(cc)}",
                _ => throw new NotSupportedException("Logic error: one of code/coding/cc should have been not null.")
            };

            static string codeToString(string? code, string? system)
            {
                var systemAddition = system is null ? string.Empty : $" (system '{system}')";
                return $"'{code ?? "(node code)"}'{systemAddition}";
            }

            static string ccToString(CodeableConcept cc) =>
                string.Join(',', cc.Coding?.Select(c => codeToString(c.Code, c.System)) ?? []);
        }


        /// <inheritdoc/>
        public JToken ToJson()
        {
            var props = new JObject(new JProperty("abstractAllowed", AbstractAllowed));
            if (Strength is not null)
                props.Add(new JProperty("strength", Strength!.GetLiteral()));

            props.Add(new JProperty("valueSet", (string)ValueSetUri));

            if (Checks != CodedContentChecks.Default)
                props.Add(new JProperty("checks", Checks.ToString()));

            return new JProperty("binding", props);
        }

        private static (Issue?, string?) interpretResults(Parameters parameters, string display)
        {
            var result = parameters.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = parameters.GetSingleValue<FhirString>("message")?.Value;

            return (result, message) switch
            {
                (true, null) => (null, null),
                (true, not null) => (Issue.TERMINOLOGY_OUTPUT_WARNING, message),
                (false, null) => (Issue.TERMINOLOGY_OUTPUT_ERROR, display.Capitalize() + " is invalid, but the terminology service provided no further details."),
                (false, not null) => (Issue.TERMINOLOGY_OUTPUT_ERROR, message)
            };
        }

        private static (Issue?, string?) callService(ValidateCodeParameters parameters, ValidationSettings ctx, string display)
        {
            try
            {
                var callParams = (Parameters)parameters.DeepCopy();
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