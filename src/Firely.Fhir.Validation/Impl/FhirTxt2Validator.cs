/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the hand-coded version of the equivalent <see cref="FhirPathValidator"/> running invariant "ext-1".
    /// </summary>
    [DataContract]
    public class FhirTxt2Validator : InvariantValidator
    {
        /// <inheritdoc/>
        public override string Key => "txt-2";

        /// <inheritdoc/>
        public override OperationOutcome.IssueSeverity? Severity => OperationOutcome.IssueSeverity.Error;

        /// <inheritdoc/>
        public override bool BestPractice => false;

        /// <inheritdoc/>
        public override string? HumanDescription => "The narrative SHALL have some non-whitespace content";

        /// <inheritdoc/>
        protected override (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationContext vc, ValidationState _)
        {
            //Check whether the narrative contains non-whitespace content.
            return (!string.IsNullOrWhiteSpace(input.Value.ToString()), null);
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastInvariant-txt2", new JObject());
    }
}