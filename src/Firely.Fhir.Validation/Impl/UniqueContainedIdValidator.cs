/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the ids of the resources in <c>DomainResource.contained</c> are unique within the
    /// containing resource.
    /// </summary>
    /// <remarks>
    /// The FHIR specification does not (yet) have an invariant for this, so it is checked by this hand-coded
    /// validator. Once the specification gets such an invariant, this validator should be removed to avoid
    /// reporting the same problem twice (see https://github.com/FirelyTeam/firely-validator-api/issues/675).
    /// </remarks>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class UniqueContainedIdValidator : IGroupValidatable
    {
        /// <inheritdoc />
        ResultReport IGroupValidatable.Validate(IEnumerable<PocoNode> input, ValidationSettings vc, ValidationState state)
        {
            var seen = new HashSet<string>();
            var reports = new List<ResultReport>();

            foreach (var contained in input)
            {
                // A contained resource without an id is not our concern here.
                if (contained.Poco is not Resource { Id: { Length: > 0 } id }) continue;

                if (!seen.Add(id))
                {
                    reports.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                        $"The id '{id}' of a contained resource is not unique: another resource in the same 'contained' list has the same id.")
                        .AsResult(state, contained, nameof(UniqueContainedIdValidator), this));
                }
            }

            return reports.Count == 0 ? ResultReport.SUCCESS : ResultReport.Combine(reports);
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state) =>
            ((IGroupValidatable)this).Validate([input], vc, state);

        /// <inheritdoc />
        public JToken ToJson() => new JProperty("uniqueContainedIds", new JObject());
    }
}
