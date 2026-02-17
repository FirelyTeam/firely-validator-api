using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class CareTeamCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                { Key: "ctm-1", Expression: "onBehalfOf.exists() implies (member.resolve().iif(empty(), true, ofType(Practitioner).exists()))" }
                                         => "onBehalfOf.exists() implies (member.resolve() is Practitioner)",

                _ => constraintElement.Expression
            };
        }
    }
}