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
    public class AllAssertion : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]      
        public IAssertion[] Members { get; private set; }
#else
        [DataMember]
        public IAssertion[] Members { get; private set; }
#endif
        public AllAssertion(IEnumerable<IAssertion> members)
        {
            Members = members.ToArray();
        }

        public AllAssertion(params IAssertion[] members) : this(members.AsEnumerable())
        {
        }

        public JToken ToJson()
        {
            return Members.Length == 1
                ? Members.First().ToJson()
                : new JProperty("all", new JArray(Members.Select(m => new JObject(m.ToJson()))));
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var result = Assertions.EMPTY;

            foreach (var member in Members.OfType<IValidatable>())
            {
                var memberResult = await member.Validate(input, vc).ConfigureAwait(false);
                if (!memberResult.Result.IsSuccessful)
                {
                    // we have found a failure result, so we do not continue with the rest anymore
                    return memberResult;
                }
                result += memberResult;
            }
            return result;
        }
    }
}
