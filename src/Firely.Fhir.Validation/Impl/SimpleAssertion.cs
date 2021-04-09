using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public abstract class SimpleAssertion : IValidatable
    {

        public virtual JToken ToJson() => new JProperty(Key, Value);

        public abstract Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state);

        public abstract string Key { get; }
        public abstract object Value { get; }
    }
}
