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
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the hand-coded version of the equivalent <see cref="FhirPathValidator"/> running invariant "ext-1".
    /// </summary>
    [DataContract]
    internal class FhirTxt1Validator : InvariantValidator
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
        protected override (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState _)
        {
            // Original expression:   "expression": "htmlChecks()"

            if (input.Value is null) return (false, null);

            switch (input.Value)
            {
                case string value:
                    {
                        var result = XHtml.IsValidNarrativeXhtml(input.Value.ToString()!, out var errors);

                        if (result)
                        {
                            return (true, null);
                        }
                        else
                        {
                            var issues = errors.Select(e => new IssueAssertion(Issue.XSD_VALIDATION_ERROR, e));
                            return (false, new ResultReport(ValidationResult.Failure, issues));
                        }
                    }
                default:
                    return (false, new ResultReport(ValidationResult.Failure,
                                                    new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                                                    $"Narrative should be of type string, but is of type ({input.Value.GetType()})")));

            }
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastInvariant-txt1", new JObject());
    }
}