using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ResourceSchemaTests
    {
        [TestMethod]
        public void FollowMetaProfileTest()
        {
            var instance = new Patient { Id = "pat1", Meta = new Meta { Profile = new[] { "profile1", "profile2", "profile3", "profile4" } } }
                .ToPocoNode();

            var result = ResourceSchema.GetMetaProfileSchemas(instance, new ValidationSettings() {SelectValidationProfiles = callback }, new ValidationState());
            result.Should().BeEquivalentTo(new Canonical[] { "userprofile2", "profile3", "profile4", "userprofile5" });

            result = ResourceSchema.GetMetaProfileSchemas(instance, new ValidationSettings() {SelectValidationProfiles = declineAll }, new ValidationState());
            result.Should().BeEmpty();

            // without a callback:
            result = ResourceSchema.GetMetaProfileSchemas(instance, new ValidationSettings(), new ValidationState());
            result.Should().BeEquivalentTo(new Canonical[] { "profile1", "profile2", "profile3", "profile4" });

            static Canonical[] callback(string location, Canonical[] orignalMetaProfiles, PocoNode input, ValidationSettings vc)
                => orignalMetaProfiles
                    .Except(new Canonical[] { "profile1" })               // exclude
                    .Select(p => p == "profile2" ? "userprofile2" : p)    // change
                    .Concat(new Canonical[] { "userprofile5" })           // add
                    .ToArray();

            static Canonical[] declineAll(string location, Canonical[] orignalMetaProfiles, PocoNode input, ValidationSettings vc) => [];
        }

        [TestMethod]
        public void GetImposedProfilesTest()
        {
            // Test with a schema that has no imposed profiles
            var sdiNoImpose = new StructureDefinitionInformation(
                canonical: "http://example.org/StructureDefinition/NoImpose",
                baseCanonicals: null,
                dataType: "Patient",
                derivation: StructureDefinitionInformation.TypeDerivationRule.Constraint,
                isAbstract: false,
                imposeProfiles: null);
            var schemaNoImpose = new ResourceSchema(sdiNoImpose);

            var result = ResourceSchema.GetImposedProfiles(schemaNoImpose);
            result.Should().BeEmpty();

            // Test with a schema that has imposed profiles
            var imposedProfiles = new Canonical[] { "http://example.org/StructureDefinition/Profile1", "http://example.org/StructureDefinition/Profile2" };
            var sdiWithImpose = new StructureDefinitionInformation(
                canonical: "http://example.org/StructureDefinition/WithImpose",
                baseCanonicals: null,
                dataType: "Patient",
                derivation: StructureDefinitionInformation.TypeDerivationRule.Constraint,
                isAbstract: false,
                imposeProfiles: imposedProfiles);
            var schemaWithImpose = new ResourceSchema(sdiWithImpose);

            result = ResourceSchema.GetImposedProfiles(schemaWithImpose);
            result.Should().BeEquivalentTo(imposedProfiles);

            // Test with multiple schemas with overlapping imposed profiles
            var imposedProfiles2 = new Canonical[] { "http://example.org/StructureDefinition/Profile2", "http://example.org/StructureDefinition/Profile3" };
            var sdiWithImpose2 = new StructureDefinitionInformation(
                canonical: "http://example.org/StructureDefinition/WithImpose2",
                baseCanonicals: null,
                dataType: "Patient",
                derivation: StructureDefinitionInformation.TypeDerivationRule.Constraint,
                isAbstract: false,
                imposeProfiles: imposedProfiles2);
            var schemaWithImpose2 = new ResourceSchema(sdiWithImpose2);

            result = ResourceSchema.GetImposedProfiles(schemaWithImpose, schemaWithImpose2);
            result.Should().HaveCount(3); // Profile1, Profile2 (once), Profile3
            result.Should().Contain("http://example.org/StructureDefinition/Profile1");
            result.Should().Contain("http://example.org/StructureDefinition/Profile2");
            result.Should().Contain("http://example.org/StructureDefinition/Profile3");
        }
    }
}