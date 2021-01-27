using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class AllAssertion : IValidatable
    {
        private readonly IAssertion[] _members;

        public AllAssertion(IEnumerable<IAssertion> assertions)
        {
            _members = assertions.ToArray();
        }

        public AllAssertion(params IAssertion[] assertions) : this(new Assertions(assertions))
        {
        }

        public JToken ToJson()
        {
            return _members.Length == 1
                ? _members.First().ToJson()
                : new JProperty("all", new JArray(_members.Select(m => new JObject(m.ToJson()))));
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var result = Assertions.EMPTY;

            foreach (var member in _members.OfType<IValidatable>())
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
