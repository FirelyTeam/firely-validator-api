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
    /// The schema builder for the <see cref="IdExpectationValidator"/>.
    /// </summary>
    internal class IdExpectationBuilder : ISchemaBuilder
    {
        private const string ID_EXPECTATION_URL = "http://hl7.org/fhir/tools/StructureDefinition/id-expectation";

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            var code = def.GetPrimitiveExtensionValue(ID_EXPECTATION_URL);

            if (code is null) yield break;

            var expectation = code switch
            {
                "optional" => IdExpectation.Optional,
                "required" => IdExpectation.Required,
                "prohibited" => IdExpectation.Prohibited,
                _ => (IdExpectation?)null
            };

            if (expectation is not null)
                yield return new IdExpectationValidator(expectation.Value);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
