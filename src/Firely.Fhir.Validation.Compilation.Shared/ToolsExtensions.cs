/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation;

/// <summary>
/// Helpers for reading the <c>hl7.fhir.uv.tools</c> extension vocabulary used on logical models
/// (e.g. CDS Hooks / Da Vinci CRD) to describe their JSON shape.
/// </summary>
internal static class ToolsExtensions
{
    // Both spellings are seen in the wild across hl7.fhir.uv.tools/CRD package versions.
    private const string JSON_NAME = "http://hl7.org/fhir/tools/StructureDefinition/json-name";
    private const string JSON_NAME_LEGACY = "http://hl7.org/fhir/tools/StructureDefinition/elementdefinition-json-name";
    private const string EXTENSION_STYLE = "http://hl7.org/fhir/tools/StructureDefinition/extension-style";
    private const string EXTENSION_STYLE_LEGACY = "http://hl7.org/fhir/tools/StructureDefinition/elementdefinition-extension-style";
    private const string JSON_PROPERTY_KEY = "http://hl7.org/fhir/tools/StructureDefinition/json-property-key";
    private const string TYPE_SPECIFIER = "http://hl7.org/fhir/tools/StructureDefinition/type-specifier";
    private const string IMPLIED_STRING_PREFIX = "http://hl7.org/fhir/tools/StructureDefinition/implied-string-prefix";

    /// <summary>
    /// The literal JSON property name for this element, if it carries a <c>json-name</c> extension
    /// (used by logical models to represent snake_case or otherwise non-FHIR-conformant member names).
    /// </summary>
    internal static string? GetJsonName(this ElementDefinition ed) =>
        ed.GetPrimitiveExtensionValue(JSON_NAME) ?? ed.GetPrimitiveExtensionValue(JSON_NAME_LEGACY);

    /// <summary>
    /// The <c>extension-style</c> code (<c>none</c>/<c>fhir-extensions</c>/<c>named-elements</c>) for this
    /// element, if present.
    /// </summary>
    internal static string? GetExtensionStyle(this ElementDefinition ed) =>
        ed.GetPrimitiveExtensionValue(EXTENSION_STYLE) ?? ed.GetPrimitiveExtensionValue(EXTENSION_STYLE_LEGACY);

    /// <summary>
    /// The name of the sibling child element whose value should be used as the JSON object key when this
    /// repeating element is rendered as a JSON object (keyed by that child) rather than a JSON array.
    /// </summary>
    internal static string? GetJsonPropertyKey(this ElementDefinition ed) =>
        ed.GetPrimitiveExtensionValue(JSON_PROPERTY_KEY);

    /// <summary>
    /// Whether this element's actual type is picked at runtime by evaluating a FHIRPath condition
    /// (per the <c>type-specifier</c> extension) rather than by a fixed <c>type[]</c> reference.
    /// </summary>
    internal static bool HasTypeSpecifier(this ElementDefinition ed) =>
        ed.Extension.Any(e => e.Url == TYPE_SPECIFIER);

    /// <summary>
    /// Whether this element's wire value omits a prefix that its declared type's own format
    /// constraint would otherwise require (per the <c>implied-string-prefix</c> extension).
    /// </summary>
    internal static bool HasImpliedStringPrefix(this ElementDefinition ed) =>
        ed.Extension.Any(e => e.Url == IMPLIED_STRING_PREFIX);

    /// <summary>
    /// Reads an extension's value as a string regardless of its declared primitive type
    /// (<c>FhirString</c>, <c>Code</c>, etc.) - unlike <c>GetStringExtension</c>, which only recognizes
    /// <c>FhirString</c> and silently returns <c>null</c> for bound <c>code</c>-typed extensions such as
    /// <c>json-property-key</c>, <c>extension-style</c> and <c>id-expectation</c>.
    /// </summary>
    internal static string? GetPrimitiveExtensionValue(this ElementDefinition ed, string url) =>
        (ed.GetExtension(url)?.Value as PrimitiveType)?.JsonValue?.ToString();
}

