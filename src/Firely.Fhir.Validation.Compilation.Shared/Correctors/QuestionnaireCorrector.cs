using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class QuestionnaireCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                // correct datatype in expression:
                { Key: "que-0", Expression: @"name.matches('[A-Z]([A-Za-z0-9_]){0,254}')" }
                                         => @"name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                
                { Key: "que-7", Expression: @"operator = 'exists' implies (answer is Boolean)" }
                                         => @"operator = 'exists' implies (answer is boolean)",
                
                _ => constraintElement.Expression
            };
        }
    }
}