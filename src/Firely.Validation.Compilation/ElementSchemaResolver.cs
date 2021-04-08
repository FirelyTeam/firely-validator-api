/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Firely.Validation.Compilation
{
    public class ElementSchemaResolver : IElementSchemaResolver // internal?
    {
        private readonly IAsyncResourceResolver _wrapped;
        private readonly ConcurrentDictionary<Uri, ElementSchema> _cache = new ConcurrentDictionary<Uri, ElementSchema>();

        public ElementSchemaResolver(IAsyncResourceResolver wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        public ElementSchema GetSchema(ElementDefinitionNavigator nav)
        {
            var schemaUri = new Uri(nav.StructureDefinition.Url, UriKind.RelativeOrAbsolute);
            return _cache.GetOrAdd(schemaUri, uri => new SchemaConverter(_wrapped).Convert(nav));
        }

        public async Task<ElementSchema?> GetSchema(Uri schemaUri)
        { // TODO lock
            if (_cache.TryGetValue(schemaUri, out ElementSchema schema))
            {
                return schema;
            }

            if (schemaUri.OriginalString.StartsWith("http://hl7.org/fhirpath/")) // Compiler magic: stop condition
            {
                schema = new ElementSchema(schemaUri);
            }
            else
            {
                if (await _wrapped.FindStructureDefinitionAsync(schemaUri.OriginalString) is StructureDefinition sd)
                {
                    schema = new SchemaConverter(_wrapped).Convert(sd);
                }
            }

            _cache.TryAdd(schemaUri, schema);
            return schema;
        }

        public void DumpCache()
        {
            foreach (var item in _cache)
            {
                Debug.WriteLine($"==== {item.Key} ====");
                Debug.WriteLine(item.Value.ToJson());
            }
        }
    }
}