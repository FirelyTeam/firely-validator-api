/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
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
        private readonly ConcurrentDictionary<Uri, ElementSchema?> _cache = new();

        public ElementSchemaResolver(IAsyncResourceResolver wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        public ElementSchema? GetSchema(ElementDefinitionNavigator nav)
        {
            var schemaUri = new Uri(nav.StructureDefinition.Url, UriKind.RelativeOrAbsolute);
            return _cache.GetOrAdd(schemaUri, uri => new SchemaConverter(_wrapped).Convert(nav));
        }

        public async Task<ElementSchema?> GetSchema(Uri schemaUri)
        {
            // Direct hit.
            if (_cache.TryGetValue(schemaUri, out ElementSchema? schema)) return schema;

            var newValue = await convertSchema(schemaUri);

            // Note that, if we were pre-empted between the TryGetValue and here, we'll just
            // not use the new schema just produced, and use whatever the other
            // thread put in the cache. So, no lock needed (which is hard with async/await in 
            // this case).
            return _cache.GetOrAdd(schemaUri, newValue);

            async Task<ElementSchema?> convertSchema(Uri uri)
            {
                var inputSd = await _wrapped.FindStructureDefinitionAsync(uri.OriginalString);
                return inputSd is not null ? new SchemaConverter(_wrapped).Convert(inputSd) : null;
            }
        }

        public void DumpCache()
        {
            foreach (var item in _cache)
            {
                Debug.WriteLine($"==== {item.Key} ====");
                Debug.WriteLine(item.Value?.ToJson() ?? "(no StructDef found for this uri)");
            }
        }
    }
}