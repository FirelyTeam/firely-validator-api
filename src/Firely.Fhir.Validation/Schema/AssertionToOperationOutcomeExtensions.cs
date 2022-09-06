﻿/* 
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
        public static OperationOutcome ToOperationOutcome(this ResultReport result)
        {
            var outcome = new OperationOutcome();

            foreach (var item in result.Evidence.OfType<IssueAssertion>())
            {
                var issue = Issue.Create(item.IssueNumber, item.Severity, item.Type ?? IssueType.Unknown);

                var location =
                item.SpecificationReference is not null ?
                    item.SpecificationReference + ": " + item.Location
                    : item.Location;

                outcome.AddIssue(item.Message, issue, location);
            }

            return outcome;
        }

        /// <summary>
        /// Removes duplicate issues from the <see cref="ResultReport"/>. This may happen if an instance is validated against
        /// profiles that overlap (or where one profile is a base of the other).
        /// </summary>
        public static ResultReport RemoveDuplicateEvidence(this ResultReport report)
        {
            var issues = report.Evidence.Distinct().ToList();  // Those assertions for which equivalence is relevant will have implemented IEqualityComparer<T>
            return new ResultReport(report.Result, issues);
        }
    }
}
