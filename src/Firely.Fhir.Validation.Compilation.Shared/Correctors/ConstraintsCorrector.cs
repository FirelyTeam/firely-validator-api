using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class ConstraintsCorrector : Corrector
{
    public override void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        correctConstraints(fhirRelease, sd.Differential);
        correctConstraints(fhirRelease, sd.Snapshot);
    }

    protected abstract void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements);

    private void correctConstraints(FhirRelease? fhirRelease, IElementList? elements)
    {
        if (elements == null || elements.Element.IsNullOrEmpty())
            return;

        var contraConstraintElements = elements.Element.SelectMany(e => e.Constraint);

        CorrectConstraints(fhirRelease, contraConstraintElements);
    }
}