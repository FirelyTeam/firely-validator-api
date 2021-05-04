/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    public class SubschemaCollector
    {
        public List<string> RequestedSchemas;
        public Dictionary<Canonical, ElementSchema> DefinedSchemas { get; private set; } = new();

        public SubschemaCollector(ElementDefinitionNavigator nav)
        {
            RequestedSchemas = nav.Elements
                .Where(e => e.ContentReference is not null)
                .Distinct()
                .Select(e => e.ContentReference)
                .ToList();
        }

        public bool NeedsSchemaFor(string anchor) => RequestedSchemas.Contains(anchor);

        public void AddSchema(ElementSchema schema)
        {
            DefinedSchemas[schema.Id] = schema;
        }

        public bool FoundSubschemas => DefinedSchemas.Any();

        public DefinitionsAssertion BuildDefinitionAssertion() => new(DefinedSchemas.Values);
    }
}
