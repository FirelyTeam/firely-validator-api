using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation;

internal class RegexCorrector(string datatype, string value) : Corrector
{
    public override void CorrectDifferential(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements, string url) => correct(elements);
    public override void CorrectSnapshot(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements) => correct(elements);

    private void correct(ICollection<ElementDefinition> elements)
    {
        // Take 2 to make sure we do not iterate over all matching elements since we are only checking on count not equaling 1!
        var valueElements = elements.Where(e => e.Path == $"{datatype}.value").Take(2); 

        if (valueElements.Count() != 1) 
            return;

        var valueElement = valueElements.First();

        if (valueElement.Type.Count == 1)
            valueElement.Type[0].SetStringExtension("http://hl7.org/fhir/StructureDefinition/regex", value);
    }
}