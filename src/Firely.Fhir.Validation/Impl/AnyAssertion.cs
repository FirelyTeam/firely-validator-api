using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    [DataContract]
    public class AnyAssertion : IValidatable, IGroupValidatable
    {
        [DataMember(Order = 0)]
        public IAssertion[] Members { get; private set; }

        public AnyAssertion(IEnumerable<IAssertion> assertions)
        {
            Members = assertions.ToArray();
        }

        public AnyAssertion(params IAssertion[] assertions) : this((IEnumerable<IAssertion>)assertions)
        {
        }

        public JToken ToJson()
        {
            return Members.Length == 1
                ? Members.First().ToJson()
                : new JProperty("any", new JArray(Members.Select(m => new JObject(m.ToJson()))));
        }


        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var validatableMembers = Members.OfType<IValidatable>();

            if (!validatableMembers.Any()) return Assertions.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (validatableMembers.Count() == 1) return await validatableMembers.First().Validate(input, vc).ConfigureAwait(false);

            var result = Assertions.EMPTY;

            foreach (var member in validatableMembers)
            {
                var singleResult = await member.Validate(input, vc).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Any() && singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result;// += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var validatableMembers = Members.OfType<IGroupValidatable>();

            if (!validatableMembers.Any()) return Assertions.SUCCESS;

            // To not pollute the output if there's just a single input, just add it to the output
            if (validatableMembers.Count() == 1) return await validatableMembers.First().Validate(input, vc).ConfigureAwait(false);

            var result = Assertions.EMPTY;

            foreach (var member in validatableMembers)
            {
                var singleResult = await member.Validate(input, vc).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Any() && singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result;// += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private async Task<Assertions> foo<T>(IEnumerable<ITypedElement> input, ValidationContext vc) where T : IValidatable, IGroupValidatable
        {
            var result = Assertions.EMPTY;

            foreach (var member in Members.OfType<T>())
            {
                var singleResult = await member.Validate(input, vc).ConfigureAwait(false);
                result += singleResult;
                if (singleResult.Result.IsSuccessful)
                {
                    // we have found a result, so we do not continue with the rest anymore
                    return singleResult;
                }
            }
            return result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.TODO, "TODO", "Any did not succeed"));
        }
    }
}
