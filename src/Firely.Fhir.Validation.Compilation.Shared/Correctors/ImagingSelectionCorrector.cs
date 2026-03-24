#if R4_AND_LATER

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation;

internal class ImagingSelectionCorrector : Corrector
{
    public override void CorrectDifferential(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements, string url) => correct(fhirRelease, elements);
    public override void CorrectSnapshot(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements) => correct(fhirRelease, elements);

    private static void correct(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements)
    {
        if (fhirRelease != FhirRelease.R5)
            return;

        var sopClassElements = elements.Where(e => e.Path == "ImagingSelection.instance.sopClass");

        foreach (var sopClassElement in sopClassElements.Where(sce => sce.Type.Count == 1 && sce.Type[0].Code != "id"))
            sopClassElement.Type[0].Code = "id";
    }
}

#endif