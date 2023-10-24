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
    /// An assertion that expresses that any of its member assertions should hold.
    /// </summary>
    [DataContract]
    public class AnyValidator : IGroupValidatable
    {
        /// <summary>
        /// The member assertions of which at least one should hold.
        /// </summary>
        [DataMember]
        public IReadOnlyList<IAssertion> Members { get; private set; }

        /// <summary>
        /// If set, this error will be added to the <see cref="ResultReport"/> before the
        /// results of all members when the Any fails (= when all members fail).
        /// </summary>
        [DataMember]
        public IssueAssertion? SummaryError { get; private set; }

        /// <summary>
        /// Construct an <see cref="AnyValidator"/> based on its members.
        /// </summary>
        public AnyValidator(IEnumerable<IAssertion> members, IssueAssertion? summaryError = null)
        {
            Members = members.ToArray();
            SummaryError = summaryError;
        }

        /// <summary>
        /// Construct an <see cref="AnyValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AnyValidator(params IAssertion[] members) : this(members.AsEnumerable(), null)
        {
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, ValidationContext, ValidationState)"/>
        public ResultReport Validate(
            IEnumerable<ITypedElement> input,
            ValidationContext vc,
            ValidationState state)
        {
            if (!Members.Any()) return ResultReport.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (Members.Count == 1) return Members[0].ValidateMany(input, vc, state);

            var result = new List<ResultReport>();

            foreach (var member in Members)
            {
                var singleResult = member.ValidateMany(input, vc, state);

                if (singleResult.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore,
                    // the result of this success is the only thing that counts.
                    return singleResult;
                }

                result.Add(singleResult);
            }

            if (SummaryError is not null)
                result.Insert(0, SummaryError.ValidateMany(input, vc, state));

            return ResultReport.FromEvidence(result);
        }

        /// <inheritdoc />
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state) => Validate(new[] { input }, vc, state);


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var members = new JArray(Members.Select(m => new JObject(m.ToJson())));

            return SummaryError is null
                ? new JProperty("anyOf", members)
                : new JProperty("anyOf", new JObject(
                    new JProperty("members", members),
                    SummaryError.ToJson()));
        }

    }
}
