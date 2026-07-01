using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation;

internal static class IssueExtensions
{
    public static OperationOutcome.IssueComponent AddInvariantExtension(this OperationOutcome.IssueComponent issue, string? key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            issue.AddExtension("http://hl7.org/fhir/StructureDefinition/operationoutcome-message-id", new FhirString(key));
        }
        return issue;
    }
}