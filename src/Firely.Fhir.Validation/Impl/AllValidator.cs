﻿/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */


using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion that expresses that all member assertions should hold.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class AllValidator : IGroupValidatable
    {
        /// <summary>
        /// The member assertions the instance should be validated against.
        /// </summary>
        [DataMember]
        public IReadOnlyList<IAssertion> Members { get; private set; }

        /// <summary>
        /// When set to true, the validation of all the Members stops as soon as a single member validates to not Success. 
        /// When set to false (default) all Members will be validated.
        /// </summary>
        [DataMember]
        public bool ShortcircuitEvaluation { get; private set; } = false;

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="shortcircuitEvaluation"></param>
        /// <param name="members"></param>
        public AllValidator(IEnumerable<IAssertion> members, bool shortcircuitEvaluation)
        {
            Members = members.ToArray();
            ShortcircuitEvaluation = shortcircuitEvaluation;
        }

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AllValidator(IEnumerable<IAssertion> members) : this(members, false) { }

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AllValidator(params IAssertion[] members) : this(members.AsEnumerable(), false)
        {
        }

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="shortcircuitEvaluation"></param>
        /// <param name="members"></param>
        public AllValidator(bool shortcircuitEvaluation, params IAssertion[] members) : this(members.AsEnumerable(), shortcircuitEvaluation)
        {
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{IScopedNode}, ValidationSettings, ValidationState)"/>
        ResultReport IGroupValidatable.Validate(
            IEnumerable<IScopedNode> input,
            ValidationSettings vc,
            ValidationState state)
        {
            if (ShortcircuitEvaluation)
            {
                var evidence = new List<ResultReport>();
                foreach (var member in Members)
                {
                    var result = member.ValidateMany(input, vc, state);
                    evidence.Add(result);
                    if (!result.IsSuccessful) break;
                }
                return ResultReport.Combine(evidence);
            }
            else
                return
                    ResultReport.Combine(Members
                        .Select(ma => ma.ValidateMany(input, vc, state)).ToList());
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings vc, ValidationState state) => ((IGroupValidatable)this).Validate(new[] { input }, vc, state);


        /// <inheritdoc />
        public JToken ToJson() =>
            new JProperty("allOf",
                  ShortcircuitEvaluation ?
                    new JObject(
                        new JProperty("shortcircuitEvaluation", ShortcircuitEvaluation),
                        new JProperty("members", new JArray(Members.Select(m => new JObject(m.ToJson()))))
                        )
                : new JArray(Members.Select(m => new JObject(m.ToJson()))));

    }
}
