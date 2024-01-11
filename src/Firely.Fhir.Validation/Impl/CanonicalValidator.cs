/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// This is an additional validator for the FHIR datatype 'canonical'. As per the FHIR specification, 
    /// canonical URLs are always expected to be absolute URIs or fragment identifiers. 
    /// The purpose of this validator is to enforce this rule and perform the necessary checks.
    /// </summary>
    [DataContract]
    internal class CanonicalValidator : IValidatable
    {
        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("canonical", new JObject());

        /// <inheritdoc/>
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            switch (input.Value)
            {
                case string value:
                    {
                        var canonical = new Canonical(value);
                        return canonical.HasAnchor || canonical.IsAbsolute
                            ? ResultReport.SUCCESS
                            : new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                                $"Canonical URLs must be absolute URLs if they are not fragment references").AsResult(state);
                    }
                default:
                    return new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE,
                                $"Primitive does not have the correct type ({input.Value.GetType()})").AsResult(state);
            }
        }
    }
}