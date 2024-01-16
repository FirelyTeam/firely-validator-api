/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
