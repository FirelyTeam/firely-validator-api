/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion that expresses that all member assertions should hold.
    /// </summary>
    [DataContract]
    internal class AllValidator : IGroupValidatable
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

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{IScopedNode}, ValidationContext, ValidationState)"/>
        public ResultReport Validate(
            IEnumerable<IScopedNode> input,
            ValidationContext vc,
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
                return ResultReport.FromEvidence(evidence);
            }
            else
                return
                    ResultReport.FromEvidence(Members
                        .Select(ma => ma.ValidateMany(input, vc, state)).ToList());
        }

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationContext vc, ValidationState state) => Validate(new[] { input }, vc, state);


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
