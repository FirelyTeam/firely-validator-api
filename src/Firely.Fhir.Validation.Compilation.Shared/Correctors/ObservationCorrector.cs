using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class ObservationCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                // correct vital-signs-vs1:
                { Key: "vs-1", Expression: @"($this as dateTime).toString().length() >= 8" }
                                        => @"$this is dateTime implies $this.toString().length() >= 10",

                _ => constraintElement.Expression
            };
        }
    }
}