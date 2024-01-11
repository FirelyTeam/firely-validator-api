/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

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

        private static RegExValidator? buildRegex(IExtendable elementDef)
        {
            var pattern =
                elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex") ?? // R4
                elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-regex"); //STU3

            return pattern is not null ? new RegExValidator(pattern) : null;
        }
    }
}
