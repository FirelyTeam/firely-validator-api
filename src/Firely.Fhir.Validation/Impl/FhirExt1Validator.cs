/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the hand-coded version of the equivalent <see cref="FhirPathValidator"/> running invariant "ext-1".
    /// </summary>
    public class FhirExt1Validator : InvariantValidator
    {
        /// <inheritdoc/>
        public override string Key => "ext-1";

        /// <inheritdoc/>
        public override OperationOutcome.IssueSeverity? Severity => OperationOutcome.IssueSeverity.Error;

        /// <inheritdoc/>
        public override bool BestPractice => false;

        /// <inheritdoc/>
        public override string? HumanDescription => "Must have either extensions or value[x], not both";

        /// <inheritdoc/>
        protected override (bool, ResultAssertion?) RunInvariant(ITypedElement input, ValidationContext vc)
        {
            // Original expression:   "expression": "extension.exists() != value.exists()",

            bool hasExtension = false;
            bool hasValue = false;

            foreach (var child in input.Children())
            {
                hasExtension |= child.Name == "extension";
                hasValue |= child.Name == "value";

                if (hasExtension && hasValue) break;
            }

            return (hasExtension != hasValue, null);
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastExt1", new JObject());
    }
}