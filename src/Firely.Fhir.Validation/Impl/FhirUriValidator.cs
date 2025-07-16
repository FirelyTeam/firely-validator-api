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
        (input is PrimitiveNode {Poco: FhirUri uri}) 
            ? uri.HasValidValue() && !string.IsNullOrWhiteSpace(uri.Value) ? ResultReport.SUCCESS : new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, $"Value '{uri}' is not a valid URI").AsResult(state, input, nameof(FhirUriValidator))
            : ResultReport.SUCCESS; // TODO remove this check when the SDK is updated

    /// <inheritdoc />
    protected override string Key => "fhirUri";

    /// <inheritdoc />
    protected override object Value => new JObject();
}