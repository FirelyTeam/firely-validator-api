/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A schema representing a FHIR Extension datatype.
    /// </summary>
    public class ExtensionSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation sdi, params IAssertion[] members) : base(sdi, members)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation sdi, IEnumerable<IAssertion> members) : base(sdi, members)
        {
            // nothing
        }

        /// <inheritdoc />
        protected override Canonical[] GetAdditionalSchemas(ITypedElement instance) =>
           instance
               .Children("url")
               .Select(ite => ite.Value)
               .OfType<string>()
               .Select(s => new Canonical(s))
               .Where(s => s.IsAbsolute)  // don't include relative references in complex extensions
               .ToArray(); // this will actually always be max one...

        /// <inheritdoc />
        protected override string FhirSchemaKind => "extension";
    }
}
