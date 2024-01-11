/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    internal static class JsonExtensions
    {
        public static JToken MakeNestedProp(this JToken t) => t is JProperty ? new JObject(t) : t;

        public static JToken ToPropValue(this ITypedElement e) => e.Value is not null ? new JValue(e.Value) : e.ToJObject();
    }
}
