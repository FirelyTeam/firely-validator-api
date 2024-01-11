/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CanonicalTests
    {
        [TestMethod]
        public void SeesAnchorandVersion()
        {
            var testee = new Canonical("http://example.org/test");
            testee.Original.Should().Be("http://example.org/test");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeFalse();
            testee.HasAnchor.Should().BeFalse();
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("http://example.org/test|3.4.5");
            testee.Original.Should().Be("http://example.org/test|3.4.5");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeTrue();
            testee.HasAnchor.Should().BeFalse();
            testee.Version.Should().Be("3.4.5");
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("http://example.org/test#anchor");
            testee.Original.Should().Be("http://example.org/test#anchor");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeFalse();
            testee.HasAnchor.Should().BeTrue();
            testee.Anchor.Should().Be("anchor");
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("http://example.org/test|3.4.5#anchor");
            testee.Original.Should().Be("http://example.org/test|3.4.5#anchor");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeTrue();
            testee.Version.Should().Be("3.4.5");
            testee.HasAnchor.Should().BeTrue();
            testee.Anchor.Should().Be("anchor");
            testee.IsAbsolute.Should().BeTrue();
        }

        [TestMethod]
        public void RecognizesUriForm()
        {
            var testee = new Canonical("urn:a:b:c");
            testee.Original.Should().Be("urn:a:b:c");
            testee.Uri.Should().Be("urn:a:b:c");
            testee.HasVersion.Should().BeFalse();
            testee.HasAnchor.Should().BeFalse();
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("local");
            testee.Original.Should().Be("local");
            testee.Uri.Should().Be("local");
            testee.HasVersion.Should().BeFalse();
            testee.HasAnchor.Should().BeFalse();
            testee.IsAbsolute.Should().BeFalse();

            testee = new Canonical("#anchor");
            testee.Original.Should().Be("#anchor");
            testee.Uri.Should().BeNull();
            testee.HasVersion.Should().BeFalse();
            testee.HasAnchor.Should().BeTrue();
            testee.Anchor.Should().Be("anchor");
            testee.IsAbsolute.Should().BeFalse();
        }

        [TestMethod]
        public void TestDeconstruction()
        {
            var (uri, version, anchor) = new Canonical("http://example.org/test|3.4.5#anchor");
            uri.Should().Be("http://example.org/test");
            version.Should().Be("3.4.5");
            anchor.Should().Be("anchor");

            (uri, version, anchor) = new Canonical("http://example.org/test");
            uri.Should().Be("http://example.org/test");
            version.Should().BeNull();
            anchor.Should().BeNull();
        }

        [TestMethod]
        public void TestConversion()
        {
            var testee = (Canonical)"http://example.org/test|3.4.5";
            testee.Original.Should().Be("http://example.org/test|3.4.5");

            var asstring = (string)testee;
            asstring.Should().Be("http://example.org/test|3.4.5");

            asstring = testee.ToString();
            asstring.Should().Be("http://example.org/test|3.4.5");

            var uri = testee.ToUri();
            uri.OriginalString.Should().Be("http://example.org/test|3.4.5");
        }

        [TestMethod]
        public void TestEquivalence()
        {
            var t1 = new Canonical("http://example.org/test|3.4.5");
            var t2 = new Canonical("http://example.org/test|3.4.5");
            var t3 = new Canonical("http://example.org/test");

            (t1 == t2).Should().BeTrue();
            t1.Equals(t2).Should().BeTrue();
            (t1 == t3).Should().BeFalse();
            t1.Equals(t3).Should().BeFalse();
        }
    }
}
