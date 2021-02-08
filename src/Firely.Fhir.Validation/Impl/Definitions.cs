using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a collection of sub-schema's that can be invoked by other assertions. 
    /// </summary>
    /// <remarks>These rules are not actively run, unless invoked by another assertion.</remarks>
    public class Definitions : IAssertion
    {
        public readonly ElementSchema[] Schemas;

        public Definitions(params ElementSchema[] schemas) : this(schemas.AsEnumerable()) { }

        public Definitions(IEnumerable<ElementSchema> schemas) => Schemas = schemas.ToArray();

        public JToken ToJson() =>
            new JProperty("definitions", new JArray(
                Schemas.Select(s => s.ToJson())));
    }
}
