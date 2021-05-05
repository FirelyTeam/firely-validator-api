/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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