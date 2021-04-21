/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    public static class ResultAssert
    {
        public static async Task Failed(this Task<ResultAssertion> result) => (await result).Failed();
        public static void Failed(this ResultAssertion result) => Assert.IsFalse(result.IsSuccessful);

        public static async Task FailedWith(this Task<ResultAssertion> result, string messageFragment) => (await result).FailedWith(messageFragment);
        public static async Task FailedWithJust(this Task<ResultAssertion> result, string messageFragment) => (await result).FailedWithJust(messageFragment);

        public static void FailedWith(this ResultAssertion result, string messageFragment)
        {
            Assert.IsTrue(!result.IsSuccessful);
            var messages = result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }
        public static void FailedWithJust(this ResultAssertion result, string messageFragment)
        {
            var issues = result.Evidence.OfType<IssueAssertion>();
            Assert.AreEqual(1, issues.Count());
            result.FailedWith(messageFragment);
        }

        public static async Task Succeeded(this Task<ResultAssertion> result) => (await result).Succeeded();
        public static void Succeeded(this ResultAssertion result) => Assert.IsTrue(result.IsSuccessful);

        public static async Task SucceededWith(this Task<ResultAssertion> result, string messageFragment) =>
            (await result).SucceededWith(messageFragment);
        public static void SucceededWith(this ResultAssertion result, string messageFragment)
        {
            Assert.IsTrue(result.IsSuccessful);
            var messages = result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }

    }
}
