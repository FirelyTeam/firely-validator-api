/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation;

/// <summary>
/// The schema builder for the <see cref="TypeSpecifierValidator"/>.
/// </summary>
internal class TypeSpecifierBuilder : ISchemaBuilder
{
    /// <inheritdoc/>
    public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
    {
        if (conversionMode == ElementConversionMode.ContentReference) yield break;

        var def = nav.Current;

        // Uses the same parsing as ElementDefinition.HasTypeSpecifier(), which TypeReferenceBuilder
        // consults to decide whether to suppress the declared type reference - so a marker we cannot
        // turn into a validator here never silently removes type validation there.
        var cases = def.GetTypeSpecifierCases();

        if (cases.Count == 0) yield break;

        // Since a type-specifier picks the actual type at runtime, it replaces the normal
        // type[]-based type reference/label rather than adding to it.
        yield return new TypeSpecifierValidator(cases);
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
