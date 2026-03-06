using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class ObservationCorrector : ConstraintsCorrector
{
    public ObservationCorrector()
    {
        // correct vital-signs-vs1:
        RegisterInvalidConstraint("Observation.effective[x]", 
                                  "vs-1",
                                  "($this as dateTime).toString().length() >= 8",
                                  "$this is dateTime implies $this.toString().length() >= 10",
                                  FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B, FhirRelease.R5);
    }
}