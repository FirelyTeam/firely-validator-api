using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class ElementDefinitionCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                // Double quotes should be single quotes
                {
                    Key: "eld-11", Expression: @"binding.empty() or type.code.empty() or type.code.contains(\"":\"") or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()"
                }
                                            => @"binding.empty() or type.code.empty() or type.code.contains(':') or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()",

                // matches should be applied on the whole string:
                { Key: "eld-19", Expression: @"path.matches('[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*')" }
                                          => @"path.matches('^[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*$')",
                
                { Key: "eld-20", Expression: @"path.matches('[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*')" }
                                          => @"path.matches('^[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*$')",
                
                _ => constraintElement.Expression
            };
        }
    }
}