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
    public class AnyValidator : IValidatable, IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]        
        public IAssertion[] Members { get; private set; }
#else        
        [DataMember]
        public IAssertion[] Members { get; private set; }
#endif

        public AnyValidator(IEnumerable<IAssertion> members)
        {
            Members = members.ToArray();
        }

        public AnyValidator(params IAssertion[] members) : this(members.AsEnumerable())
        {
        }

        public JToken ToJson() =>
            new JProperty("any", new JArray(Members.Select(m => new JObject(m.ToJson()))));


        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var validatableMembers = Members.OfType<IValidatable>();

            if (!validatableMembers.Any()) return Assertions.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (validatableMembers.Count() == 1) return await validatableMembers.First().Validate(input, vc, state).ConfigureAwait(false);

            var result = Assertions.EMPTY;

            foreach (var member in validatableMembers)
            {
                var singleResult = await member.Validate(input, vc, state).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Any() && singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result;// += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            var validatableMembers = Members.OfType<IGroupValidatable>();

            if (!validatableMembers.Any()) return Assertions.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (validatableMembers.Count() == 1) return await validatableMembers.First().Validate(input, vc, state).ConfigureAwait(false);

            var result = Assertions.EMPTY;

            foreach (var member in validatableMembers)
            {
                var singleResult = await member.Validate(input, vc, state).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Any() && singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result;// += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }
    }
}
