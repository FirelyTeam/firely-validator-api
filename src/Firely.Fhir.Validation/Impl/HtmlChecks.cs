/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implements the semantics of the FHIRPath <c>htmlChecks()</c> function
    /// (see https://build.fhir.org/fhirpath.html#fn-htmlChecks):
    /// <list type="bullet">
    /// <item>On an <c>xhtml</c> value, the content must be a <c>&lt;div&gt;</c> in the XHTML namespace that
    /// satisfies the FHIR narrative rules.</item>
    /// <item>On a <c>string</c> value, the content is treated as the content of such a <c>&lt;div&gt;</c>, so it
    /// need not (but may) have a surrounding <c>&lt;div&gt;</c> itself.</item>
    /// </list>
    /// </summary>
    internal static class HtmlChecks
    {
        /// <summary>
        /// Validates xhtml content, which must itself be a <c>&lt;div&gt;</c> in the XHTML namespace.
        /// </summary>
        public static bool CheckXhtml(string xhtml, out string? malformedError, out string[] narrativeIssues) =>
            XHtml.IsValidNarrativeXhtml(xhtml, out malformedError, out narrativeIssues);

        /// <summary>
        /// Validates string content, which is treated as the content of a <c>&lt;div&gt;</c> in the XHTML namespace.
        /// The same narrative rules apply as for xhtml, including the requirement for non-whitespace content (txt-2).
        /// </summary>
        public static bool CheckString(string content, out string? malformedError, out string[] narrativeIssues) =>
            XHtml.IsValidNarrativeXhtml(WrapInDiv(content), out malformedError, out narrativeIssues);

        /// <summary>
        /// Wraps the given content in a <c>&lt;div&gt;</c> element in the XHTML namespace.
        /// </summary>
        public static string WrapInDiv(string content) => $"<div xmlns=\"{XmlNs.XHTML}\">{content}</div>";
    }
}
