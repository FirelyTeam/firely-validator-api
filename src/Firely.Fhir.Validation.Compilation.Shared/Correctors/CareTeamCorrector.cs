#if R4_AND_LATER

using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class CareTeamCorrector : ConstraintsCorrector
{
    public CareTeamCorrector() : base("http://hl7.org/fhir/StructureDefinition/CareTeam")
    {
        RegisterInvalidConstraint("CareTeam.participant",
                                  "ctm-1",
                                  "onBehalfOf.exists() implies (member.resolve().iif(empty(), true, ofType(Practitioner).exists()))",
                                  "onBehalfOf.exists() implies (member.resolve() is Practitioner)",
                                  FhirRelease.R4);
    }
}

#endif