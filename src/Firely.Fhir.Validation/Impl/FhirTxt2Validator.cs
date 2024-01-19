/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
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
        internal override (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState _)
        {
            //Check whether the narrative contains non-whitespace content.
            return (!string.IsNullOrWhiteSpace(input.Value.ToString()), null);
        }

        /// <inheritdoc/>
        public override JToken ToJson() => new JProperty("FastInvariant-txt2", new JObject());
    }
}