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
    /// The schema builder for the <see cref="MaxLengthValidator"/>.
    /// </summary>
    internal class MaxLengthBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.MaxLength.HasValue)
                yield return new MaxLengthValidator(def.MaxLength.Value);
            else if (isFhirStringTypedAsSystemString(def))
                yield return new MaxLengthValidator(FHIR_STRING_MAX_LENGTH);
        }

        /// <summary>
        /// The maximum length of a FHIR string (1 MB), as declared by the maxLength on string.value in the core spec.
        /// </summary>
        internal const int FHIR_STRING_MAX_LENGTH = 1048576;

        private const string SYSTEM_STRING = "http://hl7.org/fhirpath/System.String";
        private const string FHIR_TYPE_EXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type";

        // In R4 and later, Element.id is typed System.String, with the fhir-type extension saying it is a FHIR string.
        // Such an element does not get the maxLength from the string.value of the FHIR string type, so we add it here.
        private static bool isFhirStringTypedAsSystemString(ElementDefinition def) =>
            def.Type.Any(t => t.Code == SYSTEM_STRING &&
                (t.GetExtension(FHIR_TYPE_EXTENSION)?.Value as PrimitiveType)?.JsonValue?.ToString() == "string");
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
