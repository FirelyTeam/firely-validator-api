using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    public static class JsonExtensions
    {
        public static JToken MakeNestedProp(this JToken t) => t is JProperty ? new JObject(t) : t;
    }
}
