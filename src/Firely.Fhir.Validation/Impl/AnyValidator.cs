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
        /// Construct an <see cref="AnyValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AnyValidator(IEnumerable<IAssertion> members)
        {
            Members = members.ToArray();
        }

        /// <summary>
        /// Construct an <see cref="AnyValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AnyValidator(params IAssertion[] members) : this(members.AsEnumerable())
        {
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, string, ValidationContext, ValidationState)"/>
        public ResultReport Validate(
            IEnumerable<ITypedElement> input,
            string groupLocation,
            ValidationContext vc,
            ValidationState state)
        {
            if (!Members.Any()) return ResultReport.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (Members.Count == 1) return Members[0].ValidateMany(input, groupLocation, vc, state);

            var result = new List<ResultReport>();

            foreach (var member in Members)
            {
                var singleResult = member.ValidateMany(input, groupLocation, vc, state);

                if (singleResult.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore,
                    // the result of this success is the only thing that counts.
                    return singleResult;
                }

                result.Add(singleResult);
            }

            return ResultReport.FromEvidence(result);
        }

        /// <inheritdoc />
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            if (!Members.Any()) return ResultReport.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (Members.Count == 1) return Members[0].ValidateOne(input, vc, state);

            var result = new List<ResultReport>();

            foreach (var member in Members)
            {
                var singleResult = member.ValidateOne(input, vc, state);

                if (singleResult.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore,
                    // the result of this success is the only thing that counts.
                    return singleResult;
                }

                result.Add(singleResult);
            }

            return ResultReport.FromEvidence(result);
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() =>
            new JProperty("anyOf", new JArray(Members.Select(m => new JObject(m.ToJson()))));

    }
}
