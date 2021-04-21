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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion that expresses that any of its member assertions should hold.
    /// </summary>
    [DataContract]
    public class AnyValidator : IGroupValidatable
    {
#if MSGPACK_KEY
        /// <summary>
        /// The member assertions of which at least one should hold.
        /// </summary>
        [DataMember(Order = 0)]        
        public IAssertion[] Members { get; private set; }
#else     
        /// <summary>
        /// The member assertions of which at least one should hold.
        /// </summary>
        [DataMember]
        public IAssertion[] Members { get; private set; }
#endif

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

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, ValidationContext, ValidationState)"/>
        public async Task<Assertions> Validate(
            IEnumerable<ITypedElement> input,
            string groupLocation,
            ValidationContext vc,
            ValidationState state)
        {
            if (!Members.Any()) return Assertions.EMPTY;

            // To not pollute the output if there's just a single input, just add it to the output
            if (Members.Length == 1) return await Members.First().Validate(input, groupLocation, vc, state).ConfigureAwait(false);

            var result = Assertions.EMPTY;

            foreach (var member in Members)
            {
                var singleResult = await member.Validate(input, groupLocation, vc, state).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Any() && singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result;// += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() =>
            new JProperty("anyOf", new JArray(Members.Select(m => new JObject(m.ToJson()))));
    }
}
