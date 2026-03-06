#if R4_AND_LATER

using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class OperationDefinitionCorrector : ConstraintsCorrector
{
    public OperationDefinitionCorrector()
    {
        // correct opd-3:
        RegisterInvalidConstraint("OperationDefinition.parameter", 
                                  "opd-3",
                                  "targetProfile.exists() implies (type = 'Reference' or type = 'canonical')",
                                  "targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))",
                                  FhirRelease.R4, FhirRelease.R4B);
        RegisterInvalidConstraint("OperationDefinition.parameter", 
                                  "opd-3",
                                  "targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))",
                                  "targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/all-resource-types'))",
                                  FhirRelease.R5);
    }
}

#endif