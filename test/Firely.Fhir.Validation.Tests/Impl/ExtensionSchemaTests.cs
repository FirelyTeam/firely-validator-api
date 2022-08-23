using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ExtensionSchemaTests
    {
        [TestMethod]
        public void FollowMetaProfileTest()
        {
            var context = ValidationContext.BuildMinimalContext();
            context.FollowExtensionUrl = callback;

            var instance =
                    new
                    {
                        url = "http://example.com/extension1",
                        value = 12
                    }.ToTypedElement();

            var result = ExtensionSchema.GetExtensionUri(instance, context);
            result.Should().BeEquivalentTo(new Canonical("http://example.com/userUrl"));

            context.FollowExtensionUrl = declineAll;
            result = ExtensionSchema.GetExtensionUri(instance, context);
            result.Should().BeNull();

            // remove the callback:
            context.FollowExtensionUrl = null;
            result = ExtensionSchema.GetExtensionUri(instance, context);
            result.Should().BeEquivalentTo(new Canonical("http://example.com/extension1"));

            static Canonical? callback(string location, Canonical? originalUrl)
                => "http://example.com/userUrl";

            static Canonical? declineAll(string location, Canonical? orignalMetaProfiles) => null;
        }
    }
}