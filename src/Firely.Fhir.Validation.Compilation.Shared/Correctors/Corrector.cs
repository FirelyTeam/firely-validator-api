using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class Corrector
{
    public void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        if (sd.Differential?.Element != null)
            CorrectElements(fhirRelease, sd, sd.Differential.Element);

        if (sd.Snapshot?.Element != null)
            CorrectSnapshot(fhirRelease, sd, sd.Snapshot.Element);
    }

    public void CorrectSnapshot(FhirRelease? fhirRelease, StructureDefinition sd, ICollection<ElementDefinition> elements)
    {
        CorrectElements(fhirRelease, sd, elements);
        CorrectSnapshotOnlyElements(fhirRelease, sd, elements);
    }

    protected virtual void CorrectElements(FhirRelease? fhirRelease, StructureDefinition sd, ICollection<ElementDefinition> elements)
    {
    }

    protected virtual void CorrectSnapshotOnlyElements(FhirRelease? fhirRelease, StructureDefinition sd, ICollection<ElementDefinition> elements)
    {
    }
}