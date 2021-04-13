﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    public static class JsonExtensions
    {
        public static JToken MakeNestedProp(this JToken t) => t is JProperty ? new JObject(t) : t;

        public static JToken ToPropValue(this ITypedElement e) => e.Value is not null ? new JValue(e.Value) : e.ToJObject();
    }
}
