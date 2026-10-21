using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// Asserts that the value of an element (converted to a string) is a valid FHIR URI.
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class FhirUriValidator : BasicValidator
{
    internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState state) =>
        // An absent value (e.g. a primitive carrying only a data-absent-reason extension) is not
        // subject to the format check; HasValidValue() rejects present-but-invalid values,
        // including empty and whitespace-only strings. A wrong literal type is reported by PrimitiveLiteralTypeValidator.
        input is PrimitiveNode {Poco: FhirUri uri} && !uri.HasValidValue() && input.GetLiteralTypeError(vc) is null
            ? new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, $"Value '{uri}' is not a valid URI").AsResult(state, input, nameof(FhirUriValidator), this)
            : ResultReport.SUCCESS;

    /// <inheritdoc />
    protected override string Key => "fhirUri";

    /// <inheritdoc />
    protected override object Value => new JObject();
}