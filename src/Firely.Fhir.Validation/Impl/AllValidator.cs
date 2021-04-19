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
    /// An assertion that expresses that all member assertions should hold.
    /// </summary>
    [DataContract]
    public class AllValidator : IValidatable, IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]      
        public IAssertion[] Members { get; private set; }
#else
        [DataMember]
        public IAssertion[] Members { get; private set; }
#endif
        public AllValidator(IEnumerable<IAssertion> members)
        {
            Members = members.ToArray();
        }

        public AllValidator(params IAssertion[] members) : this(members.AsEnumerable())
        {
        }

        public JToken ToJson() =>
            new JProperty("allOf", new JArray(Members.Select(m => new JObject(m.ToJson()))));

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var validatableMembers = Members.Where(m => m is IValidatable or IGroupValidatable);

            var result = Assertions.SUCCESS;

            foreach (var member in validatableMembers)
            {
                var memberResult = await member.Validate(input, vc, state).ConfigureAwait(false);
                //if (!memberResult.Result.IsSuccessful)
                //{
                //    // we have found a failure result, so we do not continue with the rest anymore
                //    return memberResult;
                //}
                result += memberResult;
            }
            return result;
        }

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            var validatableMembers = Members.Where(m => m is IValidatable or IGroupValidatable);

            var result = Assertions.SUCCESS;

            foreach (var member in Members.OfType<IValidatable>())
            {
                var memberResult = await member.Validate(input, vc, state).ConfigureAwait(false);
                //if (!memberResult.Result.IsSuccessful)
                //{
                //    // we have found a failure result, so we do not continue with the rest anymore
                //    return memberResult;
                //}
                result += memberResult;
            }
            return result;
        }
    }
}
