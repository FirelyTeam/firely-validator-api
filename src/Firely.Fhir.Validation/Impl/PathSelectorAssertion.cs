using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// Asserts another assertion on a subset of an instance given by a FhirPath expression. The assertion fails if the subset is empty.
    /// </summary>
    [DataContract]
    public class PathSelectorAssertion : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string Path { get; private set; }

        [DataMember(Order = 1)]
        public IAssertion Other { get; private set; }
#else
        [DataMember]
        public string Path { get; private set; }

        [DataMember]
        public IAssertion Other { get; private set; }
#endif

        public PathSelectorAssertion(string path, IAssertion other)
        {
            Path = path;
            Other = other;
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var selected = input.Select(Path);
            return selected.Any()
                ? await Other.Validate(selected, vc).ConfigureAwait(false)
                : Assertions.EMPTY + ResultAssertion.CreateFailure(new Trace("No Selection"));
        }

        public JToken ToJson()
        {
            var props = new JObject()
            {
                new JProperty("path", Path),
                new JProperty("assertion", new JObject(Other.ToJson()))

            };

            return new JProperty("pathSelector", props);
        }



    }
}
