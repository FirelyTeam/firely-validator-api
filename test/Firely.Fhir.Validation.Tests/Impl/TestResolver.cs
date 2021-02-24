using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests.Impl
{
    internal class TestResolver : IElementSchemaResolver, IEnumerable<IElementSchema>
    {
        public void Add(IElementSchema schema) => _knownSchemas.Add(schema);

        private readonly List<IElementSchema> _knownSchemas = new();

        public TestResolver()
        {

        }

        public TestResolver(IEnumerable<IElementSchema> schemas) => _knownSchemas = new(schemas);

        public List<Uri> ResolvedSchemas = new();

        public Task<IElementSchema?> GetSchema(Uri schemaUri)
        {
            ResolvedSchemas.Add(schemaUri);

            return Task.FromResult((IElementSchema?)_knownSchemas.Where(s => s.Id == schemaUri).FirstOrDefault());
        }

        public void ClearResolved() => ResolvedSchemas.Clear();
        public IEnumerator<IElementSchema> GetEnumerator() => ((IEnumerable<IElementSchema>)_knownSchemas).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_knownSchemas).GetEnumerator();
    }
}
