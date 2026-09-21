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
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;


#pragma warning disable CS0618 // NamedExtensionsValidator is evaluation-only API
namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class NamedExtensionsValidatorTests
    {
        // The JSON name and the canonical url of the defining profile are deliberately unrelated,
        // just like in Da Vinci CRD's CDSHookServicesExtensionCRDVersion.
        private const string JSON_NAME = "active";
        private const string PROFILE_URL = "http://example.org/StructureDefinition/NamedExtensionProfile";

        // A profile that only succeeds for the value we put in the instance, so that we can see it was used.
        private static TestResolver schemaResolver() => new()
        {
            new ElementSchema(PROFILE_URL, new FixedValidator(new FhirBoolean(true).ToPocoNode()))
        };

        private static PocoNode instance() => new Patient { Active = true }.ToPocoNode();

        // KnownChildNames is empty, so 'active' is treated as a named extension.
        private static readonly NamedExtensionsValidator VALIDATOR = new([]);

        [TestMethod]
        public void UsesExplicitHookWhenSet()
        {
            var resolver = schemaResolver();
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);
            vc.ConformanceResourceResolver = new ThrowingResolver();
            vc.ResolveNamedExtension = name =>
                name == JSON_NAME ? new StructureDefinition { Url = PROFILE_URL } : null;

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.IsSuccessful.Should().BeTrue();
            resolver.ResolvedSchemas.Should().Contain(PROFILE_URL);
        }

        [TestMethod]
        public void FallsBackToConformanceResolverWhenHookIsNotSet()
        {
            var resolver = schemaResolver();
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);
            vc.ConformanceResourceResolver = new NameAnsweringResolver();

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.IsSuccessful.Should().BeTrue();
            resolver.ResolvedSchemas.Should().Contain(PROFILE_URL);
        }

        [TestMethod]
        public void ThrowsWhenBothAreAbsent()
        {
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: schemaResolver());

            var validate = () => ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            validate.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ReportsUnresolvableName()
        {
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: schemaResolver());
            vc.ResolveNamedExtension = _ => null;

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.Evidence.Should().ContainSingle().Which.Should().BeOfType<IssueAssertion>().Which
                .IssueNumber.Should().Be(Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);
        }

        /// <summary>Mimics Firely Server's current workaround: the resolver chain answers bare names too.</summary>
        private class NameAnsweringResolver : IAsyncResourceResolver
        {
            public System.Threading.Tasks.Task<Resource?> ResolveByCanonicalUriAsync(string uri) =>
                System.Threading.Tasks.Task.FromResult<Resource?>(uri == JSON_NAME ? new StructureDefinition { Url = PROFILE_URL } : null);

            public System.Threading.Tasks.Task<Resource?> ResolveByUriAsync(string uri) => ResolveByCanonicalUriAsync(uri);
        }

        private class ThrowingResolver : IAsyncResourceResolver
        {
            public System.Threading.Tasks.Task<Resource?> ResolveByCanonicalUriAsync(string uri) => throw new NotSupportedException();
            public System.Threading.Tasks.Task<Resource?> ResolveByUriAsync(string uri) => throw new NotSupportedException();
        }
    }
}
#pragma warning restore CS0618
