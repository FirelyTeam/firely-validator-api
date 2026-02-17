#if R4_AND_LATER

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation;

internal class ImagingSelectionCorrector : Corrector
{
    public override void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        if (fhirRelease != FhirRelease.R5)
            return;

        correctSopClassId(sd.Differential);
        correctSopClassId(sd.Snapshot);
    }

    private static void correctSopClassId(IElementList? elements)
    {
        if (elements is null) 
            return;

        var sopClassElements = elements.Element.Where(e => e.Path == "ImagingSelection.instance.sopClass");

        foreach (var sopClassElement in sopClassElements.Where(sce => sce.Type.Count == 1 && sce.Type[0].Code != "id"))
            sopClassElement.Type[0].Code = "id";
    }
}

#endif