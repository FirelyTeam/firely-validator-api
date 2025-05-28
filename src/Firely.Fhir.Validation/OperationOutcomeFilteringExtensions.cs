using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System;
using System.Linq;

namespace Firely.Fhir.Validation;

/// <summary>
/// Helper methods for post-processing logic of <see cref="OperationOutcome"/>. 
/// </summary>
public static class OperationOutcomeFilteringExtensions
{
    /// <summary>
    /// Helper method to transform or remove issues in <see cref="OperationOutcome"/>.
    /// </summary>
    /// <param name="outcome"><see cref="OperationOutcome"/> to modify.</param>
    /// <param name="filter">Callback for each <see cref="OperationOutcome.Issue"/> allowing for modification.</param>
    /// <returns>Modified <see cref="OperationOutcome"/>.</returns>
    public static OperationOutcome TransformIssues(this OperationOutcome outcome, Func<OperationOutcome.IssueComponent, OperationOutcome.IssueComponent?> filter)
    {
        if(filter is null) throw new ArgumentNullException(nameof(filter));
        var newOutcome = new OperationOutcome();
        newOutcome.AddIssue(outcome.Issue.Select(filter));
        return newOutcome;
    }
}