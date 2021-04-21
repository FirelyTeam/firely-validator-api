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
    public static class AssertionExtensions
    {
        //public static Assertions AddResultAssertion(this Assertions assertions)
        //{
        //    return assertions.OfType<IssueAssertion>().Any() ? assertions + ResultAssertion.FAILURE : assertions + ResultAssertion.SUCCESS;
        //}

        public static IEnumerable<IssueAssertion> GetIssueAssertions(this ResultAssertion result)
            => result.Evidence.OfType<IssueAssertion>()
                .Concat(result.Evidence.OfType<ResultAssertion>().SelectMany(ra => ra.GetIssueAssertions()));

        /// <summary>
        /// Build an OperationOutcome from Assertion
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static OperationOutcome ToOperationOutcome(this ResultAssertion result)
        {
            var outcome = new OperationOutcome();

            foreach (var item in result.GetIssueAssertions())
            {
                var issue = Issue.Create(item.IssueNumber, item.Severity ?? IssueSeverity.Information, item.Type ?? IssueType.Unknown);
                outcome.AddIssue(item.Message, issue, item.Location);
            }
            return outcome;
        }
    }
}
