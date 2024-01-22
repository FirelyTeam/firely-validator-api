/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="PatternValidator"/>.
    /// </summary>
    /// <remarks>This constraint is not part of an element refering to a backbone type (see eld-5).</remarks>
    internal class PatternBuilder : ISchemaBuilder
    {

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.Pattern is not null)
            {
                var inspector = ModelInspector.ForType(def.Pattern.GetType());
                yield return new FixedValidator(def.Pattern.ToTypedElement(inspector));
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
