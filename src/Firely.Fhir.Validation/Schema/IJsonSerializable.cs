/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents an object that can represent its internal configuration as Json.
    /// </summary>
    public interface IJsonSerializable
    {
        /// <summary>
        /// Convert an instance into a Json tree.
        /// </summary>
        /// <returns></returns>
        JToken ToJson();
    }

}