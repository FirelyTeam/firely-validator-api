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
namespace Firely.Fhir.Validation.Compilation;

/// <summary>
/// The schema builder for the <see cref="TypeSpecifierValidator"/>.
/// </summary>
internal class TypeSpecifierBuilder : ISchemaBuilder
{
    private const string TYPE_SPECIFIER_URL = "http://hl7.org/fhir/tools/StructureDefinition/type-specifier";
    private const string CONDITION_URL = "condition";
    private const string TYPE_URL = "type";

    /// <inheritdoc/>
    public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
    {
        if (conversionMode == ElementConversionMode.ContentReference) yield break;

        var def = nav.Current;

        var cases = def.Extension
            .Where(e => e.Url == TYPE_SPECIFIER_URL)
            .Select(toCase)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        if (cases.Count == 0) yield break;

        // Since a type-specifier picks the actual type at runtime, it replaces the normal
        // type[]-based type reference/label rather than adding to it.
        yield return new TypeSpecifierValidator(cases);

        static TypeSpecifierCase? toCase(Extension typeSpecifier)
        {
            var condition = subExtensionValue(typeSpecifier, CONDITION_URL);
            var type = subExtensionValue(typeSpecifier, TYPE_URL);

            return condition is not null && type is not null
                ? new TypeSpecifierCase(condition, new Canonical(type))
                : null;
        }

        static string? subExtensionValue(Extension parent, string url) =>
            (parent.Extension?.Find(e => e.Url == url)?.Value as PrimitiveType)?.JsonValue?.ToString();
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
