/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Validates the XHTML content of the <c>rendering-xhtml</c> extension
    /// (<see href="http://hl7.org/fhir/StructureDefinition/rendering-xhtml"/>).
    /// Although the extension stores its value as a <c>valueString</c>, the content is
    /// semantically XHTML and is validated using the same FHIR XHTML rules that apply
    /// to regular XHTML-bearing elements such as <c>Narrative.div</c>.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class RenderingXhtmlValidator : IValidatable
    {
        /// <summary>
        /// The canonical URL of the <c>rendering-xhtml</c> extension.
        /// </summary>
        public const string RENDERING_XHTML_URL = "http://hl7.org/fhir/StructureDefinition/rendering-xhtml";

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("RenderingXhtmlValidator", new JObject());

        /// <inheritdoc/>
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            // Navigate to the value[x] child of the Extension element.
            var valueNode = input.Child("value")?.SingleOrDefault();

            // Only validate when a FhirString value is present; other types or missing values are
            // handled by regular schema validation, not here.
            if (valueNode is not PrimitiveNode { Primitive: FhirString fhirString } || fhirString.Value is null)
                return ResultReport.SUCCESS;

            var isValid = XHtml.IsValidNarrativeXhtml(fhirString.Value, out var malformedError, out var narrativeIssues);

            if (isValid) return ResultReport.SUCCESS;

            var issueReports = malformedError is null
                ? narrativeIssues.Select(e => new IssueAssertion(Issue.XSD_VALIDATION_ERROR, e).AsResult(state, valueNode)).ToArray()
                : [new IssueAssertion(Issue.XSD_VALIDATION_ERROR, malformedError).AsResult(state, valueNode)];

            return ResultReport.Combine(issueReports);
        }
    }
}
