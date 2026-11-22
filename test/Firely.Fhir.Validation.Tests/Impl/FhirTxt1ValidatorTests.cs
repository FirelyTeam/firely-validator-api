/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Tests the hand-coded htmlChecks() implementation, see https://build.fhir.org/fhirpath.html#fn-htmlChecks.
    /// </summary>
    [TestClass]
    public class FhirTxt1ValidatorTests
    {
        private const string DIV = "<div xmlns=\"http://www.w3.org/1999/xhtml\">";

        private static bool run(PocoNode input) =>
            new FhirTxt1Validator().RunInvariant(input, ValidationSettings.BuildMinimalContext(), new ValidationState()).Success;

        [TestMethod]
        [DataRow(DIV + "<p>Hello</p></div>", true, "xhtml with a div root is valid")]
        [DataRow("<p>Hello</p>", false, "xhtml must have a div root")]
        [DataRow(DIV + "<script>alert(1)</script></div>", false, "script is not allowed")]
        public void XhtmlMustBeADiv(string xhtml, bool expected, string because) =>
            run(PocoNode.ForPrimitive<XHtml>(xhtml)).Should().Be(expected, because);

        [TestMethod]
        [DataRow("<b>Bold</b> text with <i>markup</i>", true, "a string is treated as the content of a div")]
        [DataRow("<br/>", true, "a single non-div element is valid div content")]
        [DataRow("plain text", true, "plain text is valid div content")]
        [DataRow(DIV + "<b>Bold</b></div>", true, "a div is valid div content")]
        [DataRow("<script>alert(1)</script>", false, "script is not allowed")]
        [DataRow("<b>unclosed", false, "the content must be well-formed")]
        public void StringIsTreatedAsDivContent(string content, bool expected, string because) =>
            run(PocoNode.ForPrimitive<FhirString>(content)).Should().Be(expected, because);
    }
}
