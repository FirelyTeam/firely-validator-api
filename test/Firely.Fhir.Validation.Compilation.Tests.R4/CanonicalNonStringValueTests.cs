/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// A lenient parse keeps a JSON value of the wrong type in a primitive. For a canonical, that must be
    /// reported as an issue, not thrown. See issue #716.
    /// </summary>
    public class CanonicalNonStringValueTests
    {
        private const string LIBRARY_WITH_NUMERIC_CANONICAL = """
            { "resourceType": "Library", "status": "active",
              "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/library-type", "code": "logic-library" } ] },
              "relatedArtifact": [ { "type": "depends-on", "resource": 42 } ] }
            """;

        [Fact]
        public void NonStringCanonicalIsReportedAndDoesNotThrow()
        {
            var library = (Library)FhirJsonDeserializer.RECOVERABLE.DeserializeResource(LIBRARY_WITH_NUMERIC_CANONICAL);
            var resolver = new CachedResolver(ZipSource.CreateValidationSource());
            var validator = new Validator(resolver, new LocalTerminologyService(resolver.AsAsync()));

            var outcome = validator.Validate(library);

            outcome.Success.Should().BeFalse(outcome.ToString());
            var errors = outcome.Issue.Where(i => i.Severity <= OperationOutcome.IssueSeverity.Error).ToList();
            errors.Should().ContainSingle(outcome.ToString());
            outcome.ToString().Should().Contain("not the right type of literal for a canonical");
        }

        [Fact]
        public void NonStringCanonicalWithTargetProfileDoesNotThrow()
        {
            const string json = """
                { "resourceType": "ActivityDefinition", "status": "active", "library": [ 42 ] }
                """;
            var activityDefinition = (ActivityDefinition)FhirJsonDeserializer.RECOVERABLE.DeserializeResource(json);
            var resolver = new CachedResolver(ZipSource.CreateValidationSource());
            var validator = new Validator(resolver, new LocalTerminologyService(resolver.AsAsync()));

            var outcome = validator.Validate(activityDefinition);

            outcome.Success.Should().BeFalse(outcome.ToString());
            outcome.ToString().Should().Contain("not the right type of literal for a canonical");
        }
    }
}
