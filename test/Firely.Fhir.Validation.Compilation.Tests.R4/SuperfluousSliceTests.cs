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
    public class SuperfluousSliceTests
    {
        private const string PROFILE_URL = "http://example.org/fhir/StructureDefinition/SuperfluousSliceTestProfile";

        private static StructureDefinition profile(string type, params ElementDefinition[] differential) => new()
        {
            Url = PROFILE_URL,
            Name = "SuperfluousSliceTestProfile",
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
        /// A slice whose discriminator always succeeds (here: a 'profile' discriminator on an
        /// element without any profiles) is superfluous, but not wrong. See issue #662.
        /// </summary>
        [Fact]
        public void SuperfluousSliceShouldNotFailSchemaCompilation()
        {
            var sd = profile("Patient",
                new ElementDefinition("Patient.name")
                {
                    ElementId = "Patient.name",
                    Slicing = new ElementDefinition.SlicingComponent
                    {
                        Rules = ElementDefinition.SlicingRules.Open,
                        Discriminator =
                        [
                            new ElementDefinition.DiscriminatorComponent
                            {
                                Type = ElementDefinition.DiscriminatorType.Profile, Path = "$this"
                            }
                        ]
                    }
                },
                new ElementDefinition("Patient.name")
                {
                    ElementId = "Patient.name:only",
                    SliceName = "only",
                    Min = 0,
                    Max = "*"
                });

            var patient = new Patient { Name = [new HumanName { Family = "Kramer" }] };

            var outcome = validate(sd, patient);

            outcome.Success.Should().BeTrue(outcome.ToString());
        }
    }
}
