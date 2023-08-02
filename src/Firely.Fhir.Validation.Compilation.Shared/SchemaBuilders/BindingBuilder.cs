/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Validation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="BindingValidator"/>.
    /// </summary>
    internal class BindingBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {

            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.Binding?.ValueSet is not null)
#if STU3
                yield return new BindingValidator(convertSTU3Binding(def.Binding.ValueSet), convertStrength(def.Binding.Strength), true, $"{nav.StructureDefinition.Url}#{def.Path}");
#else
                yield return new BindingValidator(def.Binding.ValueSet, convertStrength(def.Binding.Strength), true, $"{nav.StructureDefinition.Url}#{def.Path}");
#endif

#if STU3
            static string convertSTU3Binding(DataType valueSet) =>
                valueSet switch
                {
                    FhirUri uri => uri.Value,
                    ResourceReference r => r.Reference,
                    _ => throw new IncorrectElementDefinitionException($"Encountered a STU3 Binding.ValueSet with an incorrect type.")
                };
#endif

        }

        private static BindingValidator.BindingStrength? convertStrength(BindingStrength? strength) => strength switch
        {
            BindingStrength.Required => BindingValidator.BindingStrength.Required,
            BindingStrength.Extensible => BindingValidator.BindingStrength.Extensible,
            BindingStrength.Preferred => BindingValidator.BindingStrength.Preferred,
            BindingStrength.Example => BindingValidator.BindingStrength.Example,
            _ => default,
        };
    }
}
