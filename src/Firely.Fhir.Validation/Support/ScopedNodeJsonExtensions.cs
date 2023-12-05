using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    internal static class ScopedNodeJsonExtensions
    {
        /// <summary>
        /// Converts a IScopedNode to a Json string.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <remarks>Be careful using this function, because we move first to an ITypedElement.</remarks>
        public static string ToJson(this IScopedNode instance)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var node = instance.AsTypedElement();
#pragma warning restore CS0618 // Type or member is obsolete
            return node.ToJObject().ToString(Newtonsoft.Json.Formatting.None);
        }

        public static JToken ToJToken(this DataType instance)
        {
            if (instance is PrimitiveType pt)
                return pt.ObjectValue is not null ? new JValue(pt.ObjectValue) : JValue.CreateNull();

            var modelInspector = ModelInspector.ForAssembly(instance.GetType().Assembly);
            return instance.ToTypedElement(modelInspector).ToJObject();
        }
    }
}