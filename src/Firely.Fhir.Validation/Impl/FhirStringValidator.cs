using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// This is an additional validator for the FHIR datatype 'string'.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif

    public class FhirStringValidator : IValidatable
    {
        // The maximum length of a FHIR string (1 MB), as declared by the maxLength on string.value.
        private const int MAX_STRING_LENGTH = 1048576;

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("string", new JObject());

        /// <inheritdoc/>
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            if (input is not PrimitiveNode { Primitive: FhirString str })
                return new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                    $"Primitive does not have the correct type ({input.Poco.TypeName})").AsResult(state, input, nameof(FhirStringValidator), this);
            // A wrong literal type (e.g. a number) is reported by PrimitiveLiteralTypeValidator.
            // A string over the 1 MB limit is also not a valid value, but that is reported by the maxLength
            // on string.value, so we only report the empty string here.
            if (!str.HasValidValue() && input.GetLiteralTypeError(vc) is null && str.JsonValue is not string { Length: > MAX_STRING_LENGTH })
                return new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                    $"String values cannot be empty").AsResult(state, input, nameof(FhirStringValidator), this);
            return ResultReport.SUCCESS;
        }
    }
}
