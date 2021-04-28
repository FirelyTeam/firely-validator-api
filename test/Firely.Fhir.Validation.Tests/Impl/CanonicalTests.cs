/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CanonicalTests
    {
        [TestMethod]
        public void TestConstruction()
        {
            var testee = new Canonical("http://example.org/test");
            testee.Original.Should().Be("http://example.org/test");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeFalse();
            testee.Version.Should().BeNull();
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("http://example.org/test|3.4.5");
            testee.Original.Should().Be("http://example.org/test|3.4.5");
            testee.Uri.Should().Be("http://example.org/test");
            testee.HasVersion.Should().BeTrue();
            testee.Version.Should().Be("3.4.5");
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("urn:a:b:c");
            testee.Original.Should().Be("urn:a:b:c");
            testee.Uri.Should().Be("urn:a:b:c");
            testee.HasVersion.Should().BeFalse();
            testee.Version.Should().BeNull();
            testee.IsAbsolute.Should().BeTrue();

            testee = new Canonical("local");
            testee.Original.Should().Be("local");
            testee.Uri.Should().Be("local");
            testee.HasVersion.Should().Be(false);
            testee.Version.Should().BeNull();
            testee.IsAbsolute.Should().BeFalse();

            testee = new Canonical("#anchor");
            testee.Original.Should().Be("#anchor");
            testee.Uri.Should().Be("#anchor");
            testee.HasVersion.Should().Be(false);
            testee.Version.Should().BeNull();
            testee.IsAbsolute.Should().BeFalse();
        }

        [TestMethod]
        public void TestDeconstruction()
        {
            var (uri, version) = new Canonical("http://example.org/test|3.4.5");
            uri.Should().Be("http://example.org/test");
            version.Should().Be("3.4.5");

            (uri, version) = new Canonical("http://example.org/test");
            uri.Should().Be("http://example.org/test");
            version.Should().BeNull();
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
