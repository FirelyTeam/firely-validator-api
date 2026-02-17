using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class Corrector
{
    public abstract void Correct(FhirRelease? fhirRelease, StructureDefinition sd);
}