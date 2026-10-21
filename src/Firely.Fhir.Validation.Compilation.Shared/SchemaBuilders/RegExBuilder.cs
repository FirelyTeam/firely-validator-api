/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="RegExValidator"/>.
    /// </summary>
    internal class RegexBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            if (buildRegex(nav.Current) is { } re) yield return re;


            foreach (var type in nav.Current.Type)
                if (buildRegex(type) is { } ret) yield return ret;
        }

        internal static bool HasRegex(ElementDefinition def) =>
            getPattern(def) is not null || def.Type.Any(t => getPattern(t) is not null);

        private static RegExValidator? buildRegex(IExtendable elementDef) =>
            getPattern(elementDef) is { } pattern ? new RegExValidator(pattern) : null;

        private static string? getPattern(IExtendable? elementDef) =>
            elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex") ?? // R4
            elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-regex"); //STU3
    }
}
#pragma warning restore CS0618 // Type or member is obsolete