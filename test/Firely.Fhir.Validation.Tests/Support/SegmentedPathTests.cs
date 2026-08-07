/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class SegmentedPathTests
    {
        [TestMethod]
        public void TokenizesSegments()
        {
            var segments = SegmentedPath.Tokenize("Bundle.entry[3].resource.category:vscat.coding");

            Assert.AreEqual(5, segments.Count);
            Assert.AreEqual(new PathSegment("Bundle", null, null), segments[0]);
            Assert.AreEqual(new PathSegment("entry", 3, null), segments[1]);
            Assert.AreEqual(new PathSegment("resource", null, null), segments[2]);
            Assert.AreEqual(new PathSegment("category", null, "vscat"), segments[3]);
            Assert.AreEqual(new PathSegment("coding", null, null), segments[4]);
        }

        [TestMethod]
        public void TokenizerKeepsChoiceTypeNamesIntact()
        {
            // "[x]" is not an index and must not be stripped from the name - the
            // ExtensionContextValidator matches definition paths containing choice names.
            var segments = SegmentedPath.Tokenize("Extension.value[x]");

            Assert.AreEqual(new PathSegment("value[x]", null, null), segments[1]);
        }
    }
}
