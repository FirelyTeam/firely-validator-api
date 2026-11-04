/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    /// <summary>
    /// Tests for the elementdefinition-profile-element extension, which points a type profile
    /// at an element within that profile instead of at its root.
    /// </summary>
    public class ProfileElementTests : IClassFixture<SchemaBuilderFixture>
    {
        private const string PROFILE_ELEMENT = "http://hl7.org/fhir/StructureDefinition/elementdefinition-profile-element";
        private const string CONTACT_PROFILE = "http://example.org/StructureDefinition/PatientWithNamedContact";
        private const string REFERRING_PROFILE = "http://example.org/StructureDefinition/PatientReferringToContact";

        private readonly SchemaBuilderFixture _fixture;

        public ProfileElementTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        private static StructureDefinition patientProfile(string url, params ElementDefinition[] differential) => new()
        {
            Url = url,
            Name = url.Split('/').Last(),
            Type = "Patient",
            Kind = StructureDefinition.StructureDefinitionKind.Resource,
            Abstract = false,
            BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Patient",
            Derivation = StructureDefinition.TypeDerivationRule.Constraint,
            Differential = new() { Element = differential.Prepend(new ElementDefinition("Patient") { ElementId = "Patient" }).ToList() }
        };

        // Patient.contact needs a name here, and may appear at most once.
        private static StructureDefinition contactProfile() => patientProfile(CONTACT_PROFILE,
            new ElementDefinition("Patient.contact") { ElementId = "Patient.contact", Max = "1" },
            new ElementDefinition("Patient.contact.name") { ElementId = "Patient.contact.name", Min = 1 });

        private static void setProfileElement(ElementDefinition element, string profileElement)
        {
            var typeRef = element.Type.Single();
#if STU3
            typeRef.ProfileElement = new FhirUri(CONTACT_PROFILE);
            typeRef.ProfileElement.SetStringExtension(PROFILE_ELEMENT, profileElement);
#else
            var profile = new Hl7.Fhir.Model.Canonical(CONTACT_PROFILE);
            profile.SetStringExtension(PROFILE_ELEMENT, profileElement);
            typeRef.ProfileElement = [profile];
#endif
        }

        // The extension is added to the snapshot, so the snapshot generator does not try to expand the type profile.
        private IElementSchemaResolver schemaResolver(StructureDefinition referring, string profileElement)
        {
            var contact = contactProfile();
            var generator = new SnapshotGenerator(_fixture.ResourceResolver);
            generator.UpdateAsync(contact).GetAwaiter().GetResult();
            generator.UpdateAsync(referring).GetAwaiter().GetResult();
            setProfileElement(referring.Snapshot!.Element.Last(e => e.Path == "Patient.contact"), profileElement);

            return StructureDefinitionToElementSchemaResolver.CreatedCached(
                new MultiResolver(new InMemoryResourceResolver(contact, referring), _fixture.ResourceResolver));
        }

        private ResultReport validate(IElementSchemaResolver resolver, Patient patient)
        {
            var settings = _fixture.NewValidationSettings();
            settings.ElementSchemaResolver = resolver;
            return resolver.GetSchema(REFERRING_PROFILE)!.Validate(patient.ToPocoNode(), settings);
        }

        private static Patient patientWithContacts(params Patient.ContactComponent[] contacts) => new() { Contact = contacts.ToList() };
        private static Patient.ContactComponent namedContact => new() { Name = new HumanName { Family = "Doe" } };
        private static Patient.ContactComponent unnamedContact => new() { Telecom = [new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "123" }] };

        private static StructureDefinition typedReferringProfile() =>
            patientProfile(REFERRING_PROFILE, new ElementDefinition("Patient.contact") { ElementId = "Patient.contact" });

        [Fact]
        public void TypeProfileValidatesAgainstReferencedElement()
        {
            var resolver = schemaResolver(typedReferringProfile(), "Patient.contact");

            // The cardinality of the referenced element (max 1) does not apply to the referring element.
            validate(resolver, patientWithContacts(namedContact, namedContact)).IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void TypeProfileFailsWhenReferencedElementConstraintIsViolated()
        {
            var resolver = schemaResolver(typedReferringProfile(), "Patient.contact");

            var result = validate(resolver, patientWithContacts(namedContact, unnamedContact));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence.OfType<IssueAssertion>().Should().Contain(i => i.Message.Contains("name"));
        }

        [Fact]
        public void UnknownProfileElementGivesClearError()
        {
            var resolver = schemaResolver(typedReferringProfile(), "Patient.doesNotExist");

            var result = validate(resolver, patientWithContacts(namedContact));
            result.IsSuccessful.Should().BeFalse();
            result.Evidence.OfType<IssueAssertion>().Should().Contain(i =>
                i.Message.Contains("Unable to locate anchor or element 'Patient.doesNotExist'") &&
                i.Message.Contains(CONTACT_PROFILE));
        }

        [Fact]
        public void ProfileDiscriminatorMatchesThroughReferencedElement()
        {
            var referring = patientProfile(REFERRING_PROFILE,
                new ElementDefinition("Patient.contact")
                {
                    ElementId = "Patient.contact",
                    Slicing = new()
                    {
                        Discriminator = [new() { Type = ElementDefinition.DiscriminatorType.Profile, Path = "$this" }],
                        Rules = ElementDefinition.SlicingRules.Open
                    }
                },
                new ElementDefinition("Patient.contact") { ElementId = "Patient.contact:named", SliceName = "named", Min = 1 });
            var resolver = schemaResolver(referring, "Patient.contact");

            // A contact only matches the slice when it conforms to the referenced Patient.contact, which requires a name.
            validate(resolver, patientWithContacts(unnamedContact, namedContact)).IsSuccessful.Should().BeTrue();
            validate(resolver, patientWithContacts(unnamedContact)).IsSuccessful.Should().BeFalse();
        }
    }
}
