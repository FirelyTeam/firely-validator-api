/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    public static class FluentResultAssertExtensions
    {
        public static async Task Failed(this Task<ResultReport> result) => (await result).Failed();
        public static void Failed(this ResultReport result) => Assert.IsFalse(result.IsSuccessful);

        public static async Task FailedWith(this Task<ResultReport> result, string messageFragment) => (await result).FailedWith(messageFragment);
        public static async Task FailedWithJust(this Task<ResultReport> result, string messageFragment) => (await result).FailedWithJust(messageFragment);

        public static void FailedWith(this ResultReport result, string messageFragment)
        {
            Assert.IsTrue(!result.IsSuccessful);
            var messages = result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }
        public static void FailedWithJust(this ResultReport result, string messageFragment)
        {
            var issues = result.Evidence.OfType<IssueAssertion>();
            Assert.AreEqual(1, issues.Count());
            result.FailedWith(messageFragment);
        }

        public static async Task FailedWith(this Task<ResultReport> result, int issueNumber) => (await result).FailedWith(issueNumber);
        public static async Task FailedWithJust(this Task<ResultReport> result, int issueNumber) => (await result).FailedWithJust(issueNumber);

        public static void FailedWith(this ResultReport result, int issueNumber)
        {
            Assert.IsTrue(!result.IsSuccessful);
            Assert.IsTrue(result.Evidence.OfType<IssueAssertion>().Any(ia => ia.IssueNumber == issueNumber));
        }
        public static void FailedWithJust(this ResultReport result, int issueNumber)
        {
            var issues = result.Evidence.OfType<IssueAssertion>();
            Assert.AreEqual(1, issues.Count());
            result.FailedWith(issueNumber);
        }

        public static async Task Succeeded(this Task<ResultReport> result) => (await result).Succeeded();
        public static void Succeeded(this ResultReport result) => Assert.IsTrue(result.IsSuccessful);

        public static async Task SucceededWith(this Task<ResultReport> result, string messageFragment) =>
            (await result).SucceededWith(messageFragment);
        public static void SucceededWith(this ResultReport result, string messageFragment)
        {
            Assert.IsTrue(result.IsSuccessful);
            var messages = result.Evidence.OfType<IssueAssertion>().Select(ia => ia.Message);
            Assert.IsTrue(messages.Any(m => m.Contains(messageFragment)), $"did not match fragment {messageFragment}, found {string.Join(", ", messages)}");
        }

    }
}
