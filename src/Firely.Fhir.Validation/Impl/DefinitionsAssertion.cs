/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a collection of sub-schema's that can be invoked by other assertions. 
    /// </summary>
    /// <remarks>These rules are not actively run, unless invoked by another assertion.</remarks>
    [DataContract]
    public class DefinitionsAssertion : IAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public readonly ElementSchema[] Schemas;
#else
        [DataMember]
        public readonly ElementSchema[] Schemas;
#endif

        public DefinitionsAssertion(params ElementSchema[] schemas) : this(schemas.AsEnumerable()) { }

        public DefinitionsAssertion(IEnumerable<ElementSchema> schemas) => Schemas = schemas.ToArray();

        public JToken ToJson() =>
            new JProperty("definitions", new JArray(
                Schemas.Select(s => s.ToJson())));
    }
}
