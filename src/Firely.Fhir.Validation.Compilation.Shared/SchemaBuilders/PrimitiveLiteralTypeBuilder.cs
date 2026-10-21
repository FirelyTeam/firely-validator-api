/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
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
    /// The schema builder for the <see cref="PrimitiveLiteralTypeValidator"/>.
    /// </summary>
    internal class PrimitiveLiteralTypeBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (nav.StructureDefinition is { Kind: StructureDefinition.StructureDefinitionKind.PrimitiveType, Derivation: StructureDefinition.TypeDerivationRule.Specialization, Type: { } type } && nav.Path == type)
                yield return new PrimitiveLiteralTypeValidator();
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
