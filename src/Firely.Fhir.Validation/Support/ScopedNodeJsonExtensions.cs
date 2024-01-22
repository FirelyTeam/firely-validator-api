/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    internal static class ScopedNodeJsonExtensions
    {
        public static JToken ToJToken(this IScopedNode instance)
        {
            if (instance.Value is not null)
                return new JValue(instance.Value);

            return instance.ToScopedNode().ToJson();
        }
    }
}