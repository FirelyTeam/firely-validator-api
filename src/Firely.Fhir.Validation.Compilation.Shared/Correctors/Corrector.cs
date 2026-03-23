using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class Corrector
{
    public void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        if (sd.Differential?.Element != null)
            Correct(fhirRelease, sd.Differential.Element);

        if (sd.Snapshot?.Element != null)
            Correct(fhirRelease, sd.Snapshot.Element);
    }

    public abstract void Correct(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements);
}