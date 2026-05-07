#if R4_AND_LATER

using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class ElementDefinitionCorrector : ConstraintsCorrector
{
    public ElementDefinitionCorrector() : base("http://hl7.org/fhir/StructureDefinition/ElementDefinition")
    {
        // matches should be applied on the whole string:
        RegisterInvalidConstraint("ElementDefinition", 
                                  "eld-19",
                                  @"path.matches('[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*')",
                                  @"path.matches('^[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*$')",
                                  FhirRelease.R4);
        RegisterInvalidConstraint("ElementDefinition", 
                                  "eld-20",
                                  @"path.matches('[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*')",
                                  @"path.matches('^[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*$')",
                                  FhirRelease.R4);

        // Double quotes should be single quotes
        RegisterInvalidConstraint("ElementDefinition", 
                                  "eld-11",
                                  @"binding.empty() or type.code.empty() or type.code.contains(\"":\"") or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()",
                                  @"binding.empty() or type.code.empty() or type.code.contains(':') or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()",
                                  FhirRelease.R5);
    }
}

#endif