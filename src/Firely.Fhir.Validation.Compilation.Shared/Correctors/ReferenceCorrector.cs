using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class ReferenceCorrector : ConstraintsCorrector
{
    public ReferenceCorrector()
    {
#if STU3
        RegisterInvalidConstraint("Reference", 
                                  "ref-1",
                                  "reference.startsWith('#').not() or (reference.substring(1).trace('url') in %resource.contained.id.trace('ids'))",
                                  "reference.exists() implies (reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource))",
                                  FhirRelease.STU3);
#else
        RegisterInvalidConstraint("Reference", 
                                  "ref-1",
                                  "reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids'))",
                                  "reference.exists() implies (reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource))",
                                  FhirRelease.R4);
        RegisterInvalidConstraint("Reference", 
                                  "ref-1",
                                  "reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource)",
                                  "reference.exists() implies (reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource))",
                                  FhirRelease.R4B);
#endif
    }
}