/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;


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
        private static TestResolver schemaResolver(string url) => new()
        {
            new ElementSchema(url, new FixedValidator(new FhirBoolean(true).ToPocoNode()))
        };

        private static PocoNode instance() => new Patient { Active = true }.ToPocoNode();

        // KnownChildNames is empty, so 'active' is treated as a named extension.
        private static readonly NamedExtensionsValidator VALIDATOR = new([]);

        [TestMethod]
        public void ValidatesAgainstMappedCanonical()
        {
            var resolver = schemaResolver(PROFILE_URL);
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);
            vc.MapNamedElement = name => name == JSON_NAME ? PROFILE_URL : null;

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.IsSuccessful.Should().BeTrue();
            resolver.ResolvedSchemas.Should().Contain(PROFILE_URL);
        }

        [TestMethod]
        public void DefaultMapperUsesNameAsCanonical()
        {
            var resolver = schemaResolver(JSON_NAME);
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: resolver);

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.IsSuccessful.Should().BeTrue();
            resolver.ResolvedSchemas.Should().Contain(JSON_NAME);
        }

        [TestMethod]
        public void ReportsUnmappableName()
        {
            var vc = ValidationSettings.BuildMinimalContext(schemaResolver: schemaResolver(PROFILE_URL));
            vc.MapNamedElement = _ => null;

            var result = ((IValidatable)VALIDATOR).Validate(instance(), vc, new ValidationState());

            result.Evidence.Should().ContainSingle().Which.Should().BeOfType<IssueAssertion>().Which
                .IssueNumber.Should().Be(Issue.UNAVAILABLE_REFERENCED_PROFILE.Code);
        }
    }
}
#pragma warning restore CS0618
