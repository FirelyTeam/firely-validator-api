using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    internal static class ScopedNodeJsonExtensions
    {
        public static JToken ToJToken(this DataType instance)
        {
            if (instance is PrimitiveType pt)
                return pt.ObjectValue is not null ? new JValue(pt.ObjectValue) : JValue.CreateNull();

            var modelInspector = ModelInspector.ForAssembly(instance.GetType().Assembly);
            return instance.ToTypedElement(modelInspector).ToJObject();
        }
    }
}