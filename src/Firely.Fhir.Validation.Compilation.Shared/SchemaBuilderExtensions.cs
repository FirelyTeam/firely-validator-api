/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
        public static ElementSchema? BuildSchema(this ISchemaBuilder schemaBuilder, StructureDefinition definition)
            => BuildSchema(schemaBuilder, ElementDefinitionNavigator.ForSnapshot(definition));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schemaBuilder"></param>
        /// <param name="nav"></param>
        /// <returns></returns>
        public static ElementSchema? BuildSchema(this ISchemaBuilder schemaBuilder, ElementDefinitionNavigator nav)
            => schemaBuilder.Build(nav).SingleOrDefault() is ElementSchema schema ? schema : null;

    }
}
