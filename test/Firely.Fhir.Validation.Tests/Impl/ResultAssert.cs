using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests.Impl
{
    public static class ResultAssert
    {
        public static async Task Failed(this Task<Assertions> result) => (await result).Failed();
        public static void Failed(this Assertions result) => Assert.IsFalse(result.Result.IsSuccessful);

        public static async Task FailedWith(this Task<Assertions> result, string messageFragment) => (await result).FailedWith(messageFragment);
        public static async Task FailedWithJust(this Task<Assertions> result, string messageFragment) => (await result).FailedWithJust(messageFragment);

        public static void FailedWith(this Assertions result, string messageFragment)
        {
            Assert.IsTrue(!result.Result.IsSuccessful);
            var messages = result.Result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }
        public static void FailedWithJust(this Assertions result, string messageFragment)
        {
            var issues = result.Result.Evidence.OfType<IssueAssertion>();
            Assert.AreEqual(1, issues.Count());
            result.FailedWith(messageFragment);
        }

        public static async Task Succeeded(this Task<Assertions> result) => (await result).Succeeded();
        public static void Succeeded(this Assertions result) => Assert.IsTrue(result.Result.IsSuccessful);

        public static async Task SucceededWith(this Task<Assertions> result, string messageFragment) => (await result).SucceededWith(messageFragment);
        public static void SucceededWith(this Assertions result, string messageFragment)
        {
            Assert.IsTrue(result.Result.IsSuccessful);
            var messages = result.Result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }

    }
}
