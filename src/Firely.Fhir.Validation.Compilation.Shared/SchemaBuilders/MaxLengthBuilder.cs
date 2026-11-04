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

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="MaxLengthValidator"/>.
    /// </summary>
    internal class MaxLengthBuilder : ISchemaBuilder
    {
        // The 1 MB limit on FHIR strings, which the core string type declares as the maxLength of string.value.
        internal const int FHIR_STRING_MAX_LENGTH = 1024 * 1024;

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.MaxLength.HasValue)
                yield return new MaxLengthValidator(def.MaxLength.Value);
            // An element id is a FHIR string, but from R4 on it is typed System.String, so it doesn't get the limit from
            // string.value (STU3 types it as string, so it does there).
            else if (isElementIdOfSystemString(def))
                yield return new MaxLengthValidator(FHIR_STRING_MAX_LENGTH);
        }

        private static bool isElementIdOfSystemString(ElementDefinition def) =>
            def.Base?.Path == "Element.id" && def.Type is [{ Code: SystemNamespaceElementSchemaResolver.SYSTEM_STRING_URI }];
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
