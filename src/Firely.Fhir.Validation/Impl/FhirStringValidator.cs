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
        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("string", new JObject());

        /// <inheritdoc/>
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            switch (input.Value)
            {
                case string value:
                    {
                        return !string.IsNullOrEmpty(value)
                            ? ResultReport.SUCCESS
                            : new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                                $"String values cannot be empty").AsResult(state);
                        // Regex from string datatype: ^[\s\S]+$
                    }
                default:
                    return new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                                $"Primitive does not have the correct type ({input.Value.GetType()})").AsResult(state);
            }
        }
    }
}
