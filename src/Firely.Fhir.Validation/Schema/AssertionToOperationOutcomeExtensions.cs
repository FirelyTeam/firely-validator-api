/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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

                var newIssueComponent = outcome.AddIssue(item.Message, issue, item.Location);

                // The definition path is always added to the outcome.
                if (item.DefinitionPath is not null)
                {
                    if (item.DefinitionPath.ToString() is {} pathString && !string.IsNullOrWhiteSpace(pathString))
                    {
                        newIssueComponent.Diagnostics = $"ElementDefinition trace: {pathString}";
                    }
                    
                    var q = item.DefinitionPath.Current;
                    while (q is not null)
                    {
                        if (q is InvokeProfileEvent { IsProfiledFhirType: true } e)
                        {
                            newIssueComponent.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-authority", new FhirUri(e.Schema.Id.ToUri()));
                            break;
                        }

                        q = q.Previous;
                    }
                }
                
                if (item.PositionInfo is { } info)
                {
                    newIssueComponent.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-line", new Integer(info.LineNumber));
                    newIssueComponent.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-col", new Integer(info.LinePosition));
                }

                if (item.IssueSource is not null)
                {
                    newIssueComponent.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-source", new FhirString(item.IssueSource));
                }

 #pragma warning disable CS0618 // Type or member is obsolete
                if (item.Assertion is InvariantValidator invariant)
                {
 #pragma warning restore CS0618 // Type or member is obsolete
                    newIssueComponent.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-message-id", new FhirString(invariant.Key));
                }
            }

            return outcome;
        }



        /// <summary>
        /// Removes duplicate issues from the <see cref="ResultReport"/>. This may happen if an instance is validated against
        /// profiles that overlap (or where one profile is a base of the other).
        /// </summary>
        internal static ResultReport RemoveDuplicateEvidence(this ResultReport report)
        {
            var issues = report.Evidence.Distinct().ToList();  // Those assertions for which equivalence is relevant will have implemented IEqualityComparer<T>
            return new ResultReport(report.Result, issues);
        }

        /// <summary>
        /// Cleans up the <see cref="ResultReport"/> by adding the slice context to the error messages and removes duplicate evidence.
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        internal static ResultReport CleanUp(this ResultReport report)
        {
            return report.addSliceContextToErrorMessages()
                         .RemoveDuplicateEvidence();
        }

        private static ResultReport addSliceContextToErrorMessages(this ResultReport report)
        {
            List<IAssertion> assertions = new();
            foreach (var assertion in report.Evidence)
            {
                // e.g. TraceAssertion
                if (assertion is not IssueAssertion issue)
                {
                    assertions.Add(assertion);
                    continue;
                }

                //for each issue, check if it has SliceInfo, and if so, add it to the message
                if (issue.DefinitionPath?.TryGetSliceInfo(out var sliceInfo) == true)
                {
                    issue.Message += $" (for slice {sliceInfo})";
                }

                assertions.Add(issue);
            }

            return new(report.Result, assertions);
        }
    }
}
