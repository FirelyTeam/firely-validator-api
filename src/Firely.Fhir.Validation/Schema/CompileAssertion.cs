using Newtonsoft.Json.Linq;
using System;

namespace Firely.Fhir.Validation
{
    public class CompileAssertion : IAssertion
    {
        public readonly string Message;

        public JToken ToJson()
        {
            throw new NotImplementedException();
        }
    }
}
