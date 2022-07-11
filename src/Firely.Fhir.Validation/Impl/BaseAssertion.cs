/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents an informational assertion that contains the base canonicals for the
    /// StructureDefinitions from which this schema is generated.
    /// </summary>
    [DataContract]
    public class BaseAssertion : IAssertion
    {
        /// <summary>
        /// The list of canonicals for the StructureDefinitions from which this schema is generated.
        /// </summary>
        [DataMember]
        public string[] BaseCanonicals { get; }

        /// <summary>
        /// Create an trace with a message and location.
        /// </summary>
        public BaseAssertion(string[] baseCanonicals)
        {
            BaseCanonicals = baseCanonicals;
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("base", new JValue(string.Join(',', BaseCanonicals)));
    }
}
