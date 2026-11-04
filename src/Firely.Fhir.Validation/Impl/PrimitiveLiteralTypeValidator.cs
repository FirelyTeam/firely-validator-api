/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Validation;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// Reports the SDK's incorrect-literal-type error for a non-string primitive literal. Range and format failures are left to
/// the regex, unless <see cref="ValidateFormat"/> is set because the element has no regex (e.g. STU3 boolean).
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class PrimitiveLiteralTypeValidator : BasicValidator
{
    /// <summary>
    /// Also report values the SDK rejects for their format or range, not only for their literal type.
    /// </summary>
    [DataMember]
    public bool ValidateFormat { get; private set; }

    /// <summary>
    /// Initializes a new PrimitiveLiteralTypeValidator.
    /// </summary>
    public PrimitiveLiteralTypeValidator(bool validateFormat = false)
    {
        ValidateFormat = validateFormat;
    }

    internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState state)
    {
        var error = ValidateFormat ? getInvalidValueError(input, vc) : input.GetLiteralTypeError(vc);

        return error is null
            ? ResultReport.SUCCESS
            : new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, error.BaseErrorMessage)
                .AsResult(state, input, nameof(PrimitiveLiteralTypeValidator), this);
    }

    private static CodedValidationException? getInvalidValueError(PocoNode input, ValidationSettings vc)
    {
        // FhirStringValidator and FhirUriValidator report the other invalid strings and uris.
        if (input is not PrimitiveNode { Primitive: { JsonValue: not null } p } || p is FhirString or FhirUri)
            return input.GetLiteralTypeError(vc);

        // Validates a copy, since Base64Binary's validation drops whitespace from the literal.
        var copy = (PrimitiveType)p.DeepCopy();
        return copy.HasValidValue() ? null : copy.Validate(vc.GetModelInspector(input), validateRecursively: false).FirstOrDefault();
    }

    /// <inheritdoc />
    protected override string Key => "primitiveLiteralType";

    /// <inheritdoc />
    protected override object Value => new JObject(new JProperty("formatCheck", ValidateFormat));
}
