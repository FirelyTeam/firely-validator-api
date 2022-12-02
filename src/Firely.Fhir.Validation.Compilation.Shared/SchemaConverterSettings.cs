/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// Settings for configuring the <see cref="SchemaConverter" />.
    /// </summary>
    /// <param name="ResourceResolver">The <see cref="IResourceResolver"/> to use when the <see cref="StructureDefinition"/> 
    /// under conversion refers to other StructureDefinitions.</param>
    internal record SchemaConverterSettings(IAsyncResourceResolver ResourceResolver)
    {
        /// <summary>
        /// A function that maps a type name found in <see cref="TypeRefComponent.Code"/> to a resolvable canonical.
        /// If not set, it will prefix the type with the standard <c>http://hl7.org/fhir/StructureDefinition</c> prefix.
        /// </summary>
        public TypeNameMapper? TypeNameMapper { get; set; }
    }
}
