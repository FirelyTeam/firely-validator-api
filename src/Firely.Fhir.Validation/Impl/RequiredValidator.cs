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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Allows specification of required members in a resource or data type.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class RequiredValidator : IValidatable
    {
        private readonly IEnumerable<string> _requiredMembers;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public IEnumerable<string> RequiredMembers => _requiredMembers;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiredMembers"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RequiredValidator(IEnumerable<string> requiredMembers)
        {
            _requiredMembers = requiredMembers ?? throw new ArgumentNullException(nameof(requiredMembers));
        }

        JToken IJsonSerializable.ToJson() => new JProperty("required", new JArray(_requiredMembers));

        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            var children = input.Children().ToList();

            var evidence = (
                from member in _requiredMembers 
                where !children.Any(c => ChildNameMatcher.NameMatches(member, c)) 
                where input.Child(member) is null
                select new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, $"Missing required member: '{member}'")
                    .AsResult(state, input, nameof(RequiredValidator))
                ).ToList();

            return ResultReport.Combine(evidence);
        }
    }
}