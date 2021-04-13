/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class TestResolver : IElementSchemaResolver, IEnumerable<ElementSchema>
    {
        public void Add(ElementSchema schema) => _knownSchemas.Add(schema);

        private readonly List<ElementSchema> _knownSchemas = new();

        public TestResolver()
        {

        }

        public TestResolver(IEnumerable<ElementSchema> schemas) => _knownSchemas = new(schemas);

        public List<Uri> ResolvedSchemas = new();

        public Task<ElementSchema?> GetSchema(Uri schemaUri)
        {
            ResolvedSchemas.Add(schemaUri);

            return Task.FromResult((ElementSchema?)_knownSchemas.Where(s => s.Id == schemaUri).FirstOrDefault());
        }

        public void ClearResolved() => ResolvedSchemas.Clear();
        public IEnumerator<ElementSchema> GetEnumerator() => ((IEnumerable<ElementSchema>)_knownSchemas).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_knownSchemas).GetEnumerator();
    }
}