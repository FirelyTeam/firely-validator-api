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
    }
}