/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
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
        public static OperationOutcome ToOperationOutcome(this ResultAssertion result)
        {
            var outcome = new OperationOutcome();

            foreach (var item in result.Evidence.OfType<IssueAssertion>())
            {
                var issue = Issue.Create(item.IssueNumber, item.Severity ?? IssueSeverity.Information, item.Type ?? IssueType.Unknown);
                outcome.AddIssue(item.Message, issue, item.Location);
            }
            return outcome;
        }
    }
}
