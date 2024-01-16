/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
    internal class FhirExt1Validator : InvariantValidator
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
        protected override (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState _)
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
        public override JToken ToJson() => new JProperty("FastInvariant-ext1", new JObject());
    }
}