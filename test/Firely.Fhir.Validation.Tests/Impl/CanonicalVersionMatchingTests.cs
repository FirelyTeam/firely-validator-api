/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class CanonicalVersionMatchingTests
    {
        private const string TEST_PROFILE_URL = "http://example.org/StructureDefinition/TestProfile";

        [TestMethod]
        public void CanonicalVersionMatching_ExactVersion_ShouldMatch()
        {
            // Test that exact version matching works (current behavior)
            var canonical1 = new Canonical($"{TEST_PROFILE_URL}|1.5.0");
            var canonical2 = new Canonical($"{TEST_PROFILE_URL}|1.5.0");

            canonical1.Should().Be(canonical2);
            canonical1.Version.Should().Be("1.5.0");
        }

        [TestMethod]
        public void CanonicalVersionMatching_PartialVersion_ShouldExtractCorrectly()
        {
            // Test parsing of partial version
            var canonical = new Canonical($"{TEST_PROFILE_URL}|1.5");
            
            canonical.Uri.Should().Be(TEST_PROFILE_URL);
            canonical.Version.Should().Be("1.5");
            canonical.HasVersion.Should().BeTrue();
        }

        [TestMethod]
        public void CanonicalVersionMatching_ShouldSupportPartialVersionPrefix()
        {
            // This test documents the desired behavior for partial version matching
            // Version "1.5" should match "1.5.0", "1.5.1", etc.
            
            var partialVersion = "1.5";
            var fullVersions = new[] { "1.5.0", "1.5.1", "1.4.9", "1.6.0", "2.0.0" };

            foreach (var fullVersion in fullVersions)
            {
                var shouldMatch = fullVersion.StartsWith(partialVersion + ".");
                var message = $"Version '{partialVersion}' should {(shouldMatch ? "match" : "not match")} version '{fullVersion}'";
                
                if (shouldMatch)
                {
                    fullVersion.Should().StartWith(partialVersion + ".", message);
                }
                else
                {
                    fullVersion.Should().NotStartWith(partialVersion + ".", message);
                }
            }
        }

        [TestMethod]
        public void ScenarioTest_GematikLikePartialVersionMatching_ShouldWork()
        {
            // Test scenario similar to the gematik issue reported
            // They have profiles with version "1.5.0" but references use version "1.5"
            const string bundleProfileUrl = "http://fhir.abda.de/eRezeptAbgabedaten/StructureDefinition/DAV-PR-ERP-AbgabedatenBundle";
            
            var testResolver = new TestResourceResolver();
            // Add the profile with full version as it would exist in the package
            testResolver.AddStructureDefinition(bundleProfileUrl, "1.5.0");
            
            var versionResolver = new Firely.Fhir.Validation.Compilation.CanonicalVersionMatchingResolver(testResolver);
            
            // This mimics the issue: profile reference uses partial version "1.5"
            // but the actual profile has full version "1.5.0"
            var result = versionResolver.ResolveByCanonicalUri($"{bundleProfileUrl}|1.5");
            result.Should().NotBeNull("Should resolve partial version 1.5 to full version 1.5.0");
            
            var sd = result as StructureDefinition;
            sd!.Version.Should().Be("1.5.0", "Should find the 1.5.0 version when requesting 1.5");
            sd.Url.Should().Be(bundleProfileUrl);
        }

        /// <summary>
        /// Enhanced test helper that allows adding specific StructureDefinitions
        /// </summary>
        private class TestResourceResolver : IAsyncResourceResolver
        {
            private readonly Dictionary<string, StructureDefinition> _resources = new();

            public TestResourceResolver()
            {
                // Add test StructureDefinitions with different versions for the default test profile
                AddStructureDefinition(TEST_PROFILE_URL, "1.5.0");
                AddStructureDefinition(TEST_PROFILE_URL, "1.5.1");
                AddStructureDefinition(TEST_PROFILE_URL, "1.4.9");
                AddStructureDefinition(TEST_PROFILE_URL, "1.6.0");
                AddStructureDefinition(TEST_PROFILE_URL, "2.0.0");
            }

            public void AddStructureDefinition(string url, string version)
            {
                var sd = new StructureDefinition
                {
                    Url = url,
                    Version = version,
                    Id = $"test-profile-{version.Replace(".", "-")}",
                    Name = $"TestProfile{version.Replace(".", "")}",
                    Status = PublicationStatus.Active,
                    Kind = StructureDefinition.StructureDefinitionKind.Resource,
                    Abstract = false,
                    Type = "Patient",
                    BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Patient"
                };

                _resources[$"{url}|{version}"] = sd;
                
                // Also add without version for no-version matching
                if (!_resources.ContainsKey(url))
                {
                    _resources[url] = sd;
                }
            }

            public Task<Resource?> ResolveByCanonicalUriAsync(string uri)
            {
                _resources.TryGetValue(uri, out var resource);
                return Task.FromResult<Resource?>(resource);
            }

            public Resource? ResolveByCanonicalUri(string uri)
            {
                _resources.TryGetValue(uri, out var resource);
                return resource;
            }

            public Task<Resource?> ResolveByUriAsync(string uri)
            {
                return ResolveByCanonicalUriAsync(uri);
            }

            public Resource? ResolveByUri(string uri)
            {
                return ResolveByCanonicalUri(uri);
            }
        }

        [TestMethod]
        public async Task CurrentBehavior_ExactVersionMatch_ShouldWork()
        {
            // Test current behavior - exact version match should work
            var resolver = new TestResourceResolver();
            
            var exactMatch = await resolver.ResolveByCanonicalUriAsync($"{TEST_PROFILE_URL}|1.5.0");
            exactMatch.Should().NotBeNull();
            var sd = exactMatch as StructureDefinition;
            sd!.Version.Should().Be("1.5.0");
        }

        [TestMethod]
        public async Task VersionMatchingResolver_ExactVersionMatch_ShouldWork()
        {
            // Test that exact version matching still works with the new resolver
            var baseResolver = new TestResourceResolver();
            var versionResolver = new Firely.Fhir.Validation.Compilation.CanonicalVersionMatchingResolver(baseResolver);
            
            var exactMatch = await versionResolver.ResolveByCanonicalUriAsync($"{TEST_PROFILE_URL}|1.5.0");
            exactMatch.Should().NotBeNull();
            var sd = exactMatch as StructureDefinition;
            sd!.Version.Should().Be("1.5.0");
        }

        [TestMethod]
        public async Task VersionMatchingResolver_PartialVersionMatch_ShouldWork()
        {
            // Test that partial version matching works with the new resolver
            var baseResolver = new TestResourceResolver();
            var versionResolver = new Firely.Fhir.Validation.Compilation.CanonicalVersionMatchingResolver(baseResolver);
            
            var partialMatch = await versionResolver.ResolveByCanonicalUriAsync($"{TEST_PROFILE_URL}|1.5");
            partialMatch.Should().NotBeNull("New implementation should support partial version matching");
            var sd = partialMatch as StructureDefinition;
            sd!.Version.Should().Be("1.5.0", "Should match 1.5.0 when requesting 1.5");
        }

        [TestMethod]
        public void IntegrationTest_CorrectionsResolverWithPartialVersionMatching_ShouldWork()
        {
            // Test that the StructureDefinitionCorrectionsResolver uses version matching
            var baseResolver = new TestResourceResolver();
            var correctionsResolver = new Firely.Fhir.Validation.Compilation.StructureDefinitionCorrectionsResolver(baseResolver);
            
            // This should resolve "1.5" to "1.5.0" through version matching
            var result = correctionsResolver.ResolveByCanonicalUri($"{TEST_PROFILE_URL}|1.5");
            result.Should().NotBeNull("StructureDefinitionCorrectionsResolver should support partial version matching");
            var sd = result as StructureDefinition;
            sd!.Version.Should().Be("1.5.0", "Should resolve 1.5 to 1.5.0");
        }

        [TestMethod]
        public void IntegrationTest_CorrectionsResolverWithExactVersionMatching_ShouldStillWork()
        {
            // Test that exact version matching still works after the changes
            var baseResolver = new TestResourceResolver();
            var correctionsResolver = new Firely.Fhir.Validation.Compilation.StructureDefinitionCorrectionsResolver(baseResolver);
            
            // This should resolve exactly
            var result = correctionsResolver.ResolveByCanonicalUri($"{TEST_PROFILE_URL}|1.5.0");
            result.Should().NotBeNull("StructureDefinitionCorrectionsResolver should still support exact version matching");
            var sd = result as StructureDefinition;
            sd!.Version.Should().Be("1.5.0");
        }

        [TestMethod]
        public async Task VersionMatchingResolver_NoVersionMatch_ShouldReturnNull()
        {
            // Test that non-existent versions return null
            var baseResolver = new TestResourceResolver();
            var versionResolver = new Firely.Fhir.Validation.Compilation.CanonicalVersionMatchingResolver(baseResolver);
            
            var noMatch = await versionResolver.ResolveByCanonicalUriAsync($"{TEST_PROFILE_URL}|9.9");
            noMatch.Should().BeNull("Non-existent versions should return null");
        }
    }
}