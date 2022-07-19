/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Extension methods to interoperate with FHIR OperationOutcome
    /// </summary>
    public static class AssertionToOperationOutcomeExtensions
    {
        /// <summary>
        /// Build an OperationOutcome from Assertion
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static OperationOutcome ToOperationOutcome(this ResultReport result)
        {
            var outcome = new OperationOutcome();

            foreach (var item in result.Evidence.OfType<IssueAssertion>())
            {
                var issue = Issue.Create(item.IssueNumber, item.Severity, item.Type ?? IssueType.Unknown);
                outcome.AddIssue(item.Message, issue, item.Location);
            }

            return outcome;
        }

        /// <summary>
        /// Removes duplicate issues from the `OperationOutcome`. This may happen if an instance is validated against
        /// profiles that overlap (or where one profile is a base of the other).
        /// </summary>
        public static void RemoveDuplicateMessages(this OperationOutcome outcome)
        {
            var comparer = new DuplicateIssueComparer();
            outcome.Issue = outcome.Issue.Distinct(comparer).ToList();
        }

        private class DuplicateIssueComparer : IEqualityComparer<OperationOutcome.IssueComponent>
        {
            public bool Equals(OperationOutcome.IssueComponent? x, OperationOutcome.IssueComponent? y)
            {
                return (x, y) switch
                {
                    (null, null) => true,
                    (null, _) or (_, null) => false,
                    _ => x.Location?.FirstOrDefault() == y.Location?.FirstOrDefault() && x.Details?.Text == y.Details?.Text
                };
            }

            public int GetHashCode(OperationOutcome.IssueComponent issue)
            {
                var hash = unchecked(issue?.Location?.FirstOrDefault()?.GetHashCode() ^ issue?.Details?.Text?.GetHashCode());
                return (hash is null) ? 0 : hash.Value;
            }

        }
    }
}
