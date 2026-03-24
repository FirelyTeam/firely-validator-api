using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class Corrector
{
    public void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        if (sd.Differential?.Element != null)
            CorrectDifferential(fhirRelease, sd.Differential.Element, sd.Url);

        if (sd.Snapshot?.Element != null)
            CorrectSnapshot(fhirRelease, sd.Snapshot.Element);
    }

    public abstract void CorrectDifferential(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements, string url);
    public abstract void CorrectSnapshot(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements);
}