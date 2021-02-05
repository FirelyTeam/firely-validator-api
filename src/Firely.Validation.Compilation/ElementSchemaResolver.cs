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
    public class ElementSchemaResolver : IAsyncResourceResolver, ISchemaResolver // internal?
    {
        private readonly IAsyncResourceResolver _wrapped;
        private readonly ConcurrentDictionary<Uri, IElementSchema> _cache = new ConcurrentDictionary<Uri, IElementSchema>();

        public ElementSchemaResolver(IAsyncResourceResolver wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        public IElementSchema GetSchema(ElementDefinitionNavigator nav)
        {
            var schemaUri = new Uri(nav.StructureDefinition.Url, UriKind.RelativeOrAbsolute);
            return _cache.GetOrAdd(schemaUri, uri => new SchemaConverter(this).Convert(nav));
        }

        public async Task<IElementSchema> GetSchema(Uri schemaUri)
        { // TODO lock
            if (_cache.TryGetValue(schemaUri, out IElementSchema schema))
            {
                return schema;
            }

            if (schemaUri.OriginalString.StartsWith("http://hl7.org/fhirpath/")) // Compiler magic: stop condition
            {
                schema = new ElementSchema(schemaUri);
            }
            else
            {
                if (await this.FindStructureDefinitionAsync(schemaUri.OriginalString) is StructureDefinition sd)
                {
                    schema = new SchemaConverter(this).Convert(sd);
                }
            }

            _cache.TryAdd(schemaUri, schema);
            return schema;
        }

        public async Task<Resource> ResolveByCanonicalUriAsync(string uri) => await _wrapped.ResolveByCanonicalUriAsync(uri);

        public async Task<Resource> ResolveByUriAsync(string uri) => await _wrapped.ResolveByUriAsync(uri);

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