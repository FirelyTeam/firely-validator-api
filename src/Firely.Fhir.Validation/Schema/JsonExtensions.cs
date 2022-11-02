/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    internal static class JsonExtensions
    {
        public static JToken MakeNestedProp(this JToken t) => t is JProperty ? new JObject(t) : t;

        public static JToken ToPropValue(this ROD r) => r.GetValue() is { } v ? new JValue(v) : r.ToJObject();
    }
}
