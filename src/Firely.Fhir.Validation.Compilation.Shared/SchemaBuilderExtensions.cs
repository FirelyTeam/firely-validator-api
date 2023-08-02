/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    internal static class SchemaBuilderExtensions
    {
        /// <summary>
        /// Converts a <see cref="StructureDefinition"/> to an <see cref="ElementSchema"/>.
        /// </summary>
        public static ElementSchema? Build(this ISchemaBuilder schemaBuilder, StructureDefinition definition)
            => Build(schemaBuilder, ElementDefinitionNavigator.ForSnapshot(definition));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schemaBuilder"></param>
        /// <param name="nav"></param>
        /// <returns></returns>
        public static ElementSchema? Build(this ISchemaBuilder schemaBuilder, ElementDefinitionNavigator nav)
            => schemaBuilder.Build(nav).SingleOrDefault() is ElementSchema schema ? schema : null;

    }
}
