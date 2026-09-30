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
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    public class TargetProfileOnNonReferenceTests
    {
        private const string PROFILE_URL = "http://example.org/fhir/StructureDefinition/TargetProfileOnNonReferenceTestProfile";

        private static StructureDefinition profile(string type, params ElementDefinition[] differential) => new()
        {
            Url = PROFILE_URL,
            Name = "TargetProfileOnNonReferenceTestProfile",
            Status = PublicationStatus.Draft,
            FhirVersion = FHIRVersion.N4_0_1,
            Kind = StructureDefinition.StructureDefinitionKind.Resource,
            Abstract = false,
            Type = type,
            BaseDefinition = $"http://hl7.org/fhir/StructureDefinition/{type}",
            Derivation = StructureDefinition.TypeDerivationRule.Constraint,
            Differential = new StructureDefinition.DifferentialComponent { Element = differential.ToList() }
        };

        private static OperationOutcome validate(StructureDefinition sd, Resource instance)
        {
            var source = new SnapshotSource(new MultiResolver(new InMemoryProfileResolver(sd), ZipSource.CreateValidationSource()));
            var validator = new Validator(source.AsAsync(), new LocalTerminologyService(source.AsAsync()));
            return validator.Validate(instance, PROFILE_URL);
        }

        /// <summary>
        /// A targetProfile on an element that is not a reference is meaningless, but should not
        /// prevent the profile from being compiled. See issue #423.
        /// </summary>
        [Fact]
        public void TargetProfileOnNonReferenceTypeShouldBeIgnored()
        {
            var sd = profile("Patient",
                new ElementDefinition("Patient.birthDate")
                {
                    ElementId = "Patient.birthDate",
                    Type =
                    [
                        new ElementDefinition.TypeRefComponent
                        {
                            Code = "date",
                            TargetProfile = ["http://hl7.org/fhir/StructureDefinition/Patient"]
                        }
                    ]
                });

            var patient = new Patient { BirthDate = "1974-12-25" };

            var outcome = validate(sd, patient);

            outcome.Success.Should().BeTrue(outcome.ToString());
        }
    }
}
