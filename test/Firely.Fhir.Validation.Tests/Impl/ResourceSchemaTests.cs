using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ResourceSchemaTests
    {
        [TestMethod]
        public void FollowMetaProfileTest()
        {
            var context = ValidationContext.BuildMinimalContext();
            context.FollowMetaProfile = callback;

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
            result.Should().BeEquivalentTo(new Canonical[] { "userprofile", "profile3", "profile4" });

            context.FollowMetaProfile = declineAll;
            result = ResourceSchema.GetMetaProfileSchemas(instance, context);
            result.Should().BeEmpty();

            // remove the callback:
            context.FollowMetaProfile = null;
            result = ResourceSchema.GetMetaProfileSchemas(instance, context);
            result.Should().BeEquivalentTo(new Canonical[] { "profile1", "profile2", "profile3", "profile4" });

            static MetaProfileHandling callback(Canonical profile)
                => (string)profile switch
                {
                    "profile1" => new(ActionType.Decline),
                    "profile2" => new(ActionType.Accept, "userprofile"),
                    _ => new(ActionType.Accept)
                };

            static MetaProfileHandling declineAll(Canonical profile) => new(ActionType.Decline);
        }
    }
}