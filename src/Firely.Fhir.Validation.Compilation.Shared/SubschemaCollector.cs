/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// A container class that is used by the <see cref="SchemaBuilder"/> to keep track of
    /// backbones that need to be turned into individual schemas so they can be referred to
    /// by <see cref="ElementDefinition.ContentReference"/>.
    /// </summary>
    internal class SubschemaCollector
    {
        /// <summary>
        /// The list of content reference values encountered in the current list of
        /// elements in the ElementNavigator.
        /// </summary>
        public List<string> RequestedSchemas;

        /// <summary>
        /// The list of schemas that have been generated from Backbones to represent a target
        /// for the <see cref="ElementDefinition.ContentReference"/>s.
        /// </summary>
        public Dictionary<Canonical, ElementSchema> DefinedSchemas { get; private set; } = new();

        /// <summary>
        /// Initializes a collector for the given <see cref="ElementDefinitionNavigator"/>.
        /// </summary>
        /// <param name="nav"></param>
        public SubschemaCollector(ElementDefinitionNavigator nav)
        {
            RequestedSchemas = nav.Elements
                .Where(e => e.ContentReference is not null)
                .Distinct()
                .Select(e => e.ContentReference)
                .ToList();
        }

        /// <summary>
        /// Whether the current StructureDefinition contains a content reference for the
        /// given anchor, and so would need a separately defined schema for the content
        /// reference in <paramref name="anchor"/>.
        /// </summary>
        public bool NeedsSchemaFor(string anchor) => RequestedSchemas.Contains(anchor);

        /// <summary>
        /// Adds a schema for the given backbone to this collection.
        /// </summary>
        /// <param name="schema"></param>
        public void AddSchema(ElementSchema schema)
        {
            DefinedSchemas[schema.Id] = schema;
        }

        /// <summary>
        /// Whether this collection contains any subschemas used for representing
        /// backbones.
        /// </summary>
        public bool FoundSubschemas => DefinedSchemas.Any();

        /// <summary>
        /// Builds a new <see cref="DefinitionsAssertion"/> that contains the
        /// elementschema's in this collection that serve as reuseable types
        /// for referenced backbones.
        /// </summary>
        /// <returns></returns>
        public DefinitionsAssertion BuildDefinitionAssertion() => new(DefinedSchemas.Values);
    }
}
