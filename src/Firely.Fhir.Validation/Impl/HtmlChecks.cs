/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Xml.Schema;

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
        /// </summary>
        /// <remarks>Unlike <see cref="CheckXhtml"/>, this does not require the content to contain non-whitespace text
        /// (the narrative-specific rule of txt-2), so content like a single <c>&lt;br/&gt;</c> is accepted.</remarks>
        public static bool CheckString(string content, out string? malformedError, out string[] narrativeIssues)
        {
            try
            {
                var doc = SerializationUtil.XDocumentFromXmlText(WrapInDiv(content));
                malformedError = null;

                var issues = new List<string>();
                doc.Validate(SerializationUtil.BASEFHIRSCHEMAS.CompiledSchemas, (_, a) => issues.Add(a.Message));
                narrativeIssues = issues.ToArray();
                return narrativeIssues.Length == 0;
            }
            catch (FormatException fe)
            {
                malformedError = fe.Message;
                narrativeIssues = [];
                return false;
            }
        }

        /// <summary>
        /// Wraps the given content in a <c>&lt;div&gt;</c> element in the XHTML namespace.
        /// </summary>
        public static string WrapInDiv(string content) => $"<div xmlns=\"{XmlNs.XHTML}\">{content}</div>";
    }
}
