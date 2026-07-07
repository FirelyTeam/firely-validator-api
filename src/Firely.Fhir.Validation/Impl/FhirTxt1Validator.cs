/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the hand-coded version of the equivalent <see cref="FhirPathValidator"/> running invariant "ext-1".
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class FhirTxt1Validator : InvariantValidator
    {
        /// <inheritdoc/>
        public override string Key => "txt-1";

        /// <inheritdoc/>
        public override OperationOutcome.IssueSeverity? Severity => OperationOutcome.IssueSeverity.Error;

        /// <inheritdoc/>
        public override bool BestPractice => false;

        /// <inheritdoc/>
        public override string? HumanDescription => "The narrative SHALL contain only the basic html formatting elements and attributes described in chapters 7-11 (except section 4 of chapter 9) and 15 of the HTML 4.0 standard, <a> elements (either name or href), images and internally contained style attributes";

        /// <inheritdoc/>
        internal override InvariantResult RunInvariant(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            // Original expression:   "expression": "htmlChecks()"

            if (input is not PrimitiveNode primitive) return new(false, null);

            if (primitive is not {Poco: XHtml xhtml})
                return new(false, 
                    new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                        $"Narrative should be of type string, but is of type ({primitive.Poco.GetType()})").AsResult(state, input, nameof(FhirTxt1Validator), this));
        
            // Check if the narrative contains only the basic HTML formatting elements and attributesvar result = XHtml.IsValidNarrativeXhtml(input.Value.ToString()!, out var malformedError, out var narrativeIssues);

            var result = XHtml.IsValidNarrativeXhtml(xhtml.ToString()!, out var malformedError, out var narrativeIssues);
            
            if (result)
            {
                return new(true, null);
            }
            else
            {
                var issueReports = malformedError is null 
                    ? narrativeIssues.Select(e => new IssueAssertion(Issue.XSD_VALIDATION_ERROR, e).AsResult(state, input, nameof(FhirTxt1Validator), this)).ToArray()
                    : [new IssueAssertion(Issue.XSD_VALIDATION_ERROR, malformedError).AsResult(state, input, nameof(FhirTxt1Validator), this)];
                return new(false, ResultReport.Combine(issueReports));
            }
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastInvariant-txt1", new JObject());
    }
}