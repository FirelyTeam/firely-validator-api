using FluentAssertions;
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
            var context = ValidationContext.BuildMinimalContext();
            context.SelectMetaProfiles = callback;

            var instance = new
            {
                resourceType = "Patient",
                id = "pat1",
                meta = new
                {
                    profile = new[] { "profile1", "profile2", "profile3", "profile4" }
                }
            }.ToTypedElement();

            var result = ResourceSchema.GetMetaProfileSchemas(instance, context);
            result.Should().BeEquivalentTo(new Canonical[] { "userprofile2", "profile3", "profile4", "userprofile5" });

            context.SelectMetaProfiles = declineAll;
            result = ResourceSchema.GetMetaProfileSchemas(instance, context);
            result.Should().BeEmpty();

            // remove the callback:
            context.SelectMetaProfiles = null;
            result = ResourceSchema.GetMetaProfileSchemas(instance, context);
            result.Should().BeEquivalentTo(new Canonical[] { "profile1", "profile2", "profile3", "profile4" });

            static Canonical[] callback(string location, Canonical[] orignalMetaProfiles)
                => orignalMetaProfiles
                    .Except(new Canonical[] { "profile1" })               // exclude
                    .Select(p => p == "profile2" ? "userprofile2" : p)    // change
                    .Concat(new Canonical[] { "userprofile5" })           // add
                    .ToArray();

            static Canonical[] declineAll(string location, Canonical[] orignalMetaProfiles) => Array.Empty<Canonical>();
        }
    }
}