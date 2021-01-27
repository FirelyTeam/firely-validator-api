using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class AnyAssertion : IValidatable, IGroupValidatable
    {
        private readonly IAssertion[] _members;

        public AnyAssertion(IEnumerable<IAssertion> assertions)
        {
            _members = assertions.ToArray();
        }

        public JToken ToJson()
        {
            return _members.Length == 1
                ? _members.First().ToJson()
                : new JProperty("any", new JArray(_members.Select(m => new JObject(m.ToJson()))));
        }


        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var validatableMembers = _members.OfType<IValidatable>();

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
            var validatableMembers = _members.OfType<IGroupValidatable>();

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

            foreach (var member in _members.OfType<T>())
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
