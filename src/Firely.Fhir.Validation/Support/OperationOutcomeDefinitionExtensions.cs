/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal static class OperationOutcomeDefinitionExtensions
    {
        public const string OPERATION_OUTCOME_SDREF = "http://fire.ly/dotnet-sdk-operation-outcome-structdef-reference";

        public static void SetStructureDefinitionPath(this OperationOutcome.IssueComponent ic, string definitionPath)
        {
            ic.Details.Coding.RemoveAll(c => c.System == OPERATION_OUTCOME_SDREF);
            ic.Details.Coding.Add(new Coding(OPERATION_OUTCOME_SDREF, definitionPath));
        }

        public static string? GetStructureDefinitionPath(this OperationOutcome.IssueComponent ic) =>
            ic.Details.Coding.FirstOrDefault(c => c.System == OPERATION_OUTCOME_SDREF)?.Code;
    }
}
