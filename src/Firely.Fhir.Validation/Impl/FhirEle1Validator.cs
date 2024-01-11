/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents the hand-coded version of the equivalent <see cref="FhirPathValidator"/> running invariant "ele-1".
    /// </summary>
    [DataContract]
    internal class FhirEle1Validator : InvariantValidator
    {
        /// <inheritdoc/>
        public override string Key => "ele-1";

        /// <inheritdoc/>
        public override OperationOutcome.IssueSeverity? Severity => OperationOutcome.IssueSeverity.Error;

        /// <inheritdoc/>
        public override bool BestPractice => false;

        /// <inheritdoc/>
        public override string? HumanDescription => "All FHIR elements must have a @value or children";

        /// <inheritdoc/>
        protected override (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState _)
        {
            // Original R4B expression:   "expression": "hasValue() or (children().count() > id.count()) or $this is Parameters",

            // Shortcut the evaluation if there is a value
            if (input.Value is not null) return (true, null);

            // Shortcut the evaluation if this is a Parameters object
            if (input.InstanceType == "Parameters") return (true, null);

            var hasOtherChildrenThanId = input.Children().SkipWhile(c => c.Name == "id").Any();

            return (hasOtherChildrenThanId, null);
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastInvariant-ele1", new JObject());
    }
}