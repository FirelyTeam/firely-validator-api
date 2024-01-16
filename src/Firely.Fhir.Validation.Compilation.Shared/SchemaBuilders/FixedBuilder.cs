/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="FixedValidator"/>.
    /// </summary>
    internal class FixedBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.Fixed is not null)
                yield return new FixedValidator(def.Fixed);
        }
    }
}
