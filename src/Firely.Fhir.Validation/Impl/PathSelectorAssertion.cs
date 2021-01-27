﻿using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class PathSelectorAssertion : IValidatable
    {
        private readonly string _path;
        private readonly IAssertion _other;

        public PathSelectorAssertion(string path, IAssertion other)
        {
            _path = path;
            _other = other;
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var selected = input.Select(_path);
            return selected.Any()
                ? await _other.Validate(selected, vc).ConfigureAwait(false)
                : Assertions.EMPTY + ResultAssertion.CreateFailure(new Trace("No Selection"));
        }

        public JToken ToJson()
        {
            var props = new JObject()
            {
                new JProperty("path", _path),
                new JProperty("assertion", new JObject(_other.ToJson()))

            };

            return new JProperty("pathSelector", props);
        }



    }
}
