using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class StructureDefinitionCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                {
                    Key: "sdf-0", Expression: @"name.matches('[A-Z]([A-Za-z0-9_]){0,254}')" or
                                              @"name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')"
                }
                                           => @"name.exists() implies name.matches('^[A-Z]([A-Za-z0-9_]){0,254}$')",

                // do not use $this (see https://jira.hl7.org/browse/FHIR-37761)
                { Key: "sdf-24", Expression: @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                          => @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.id.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                
                { Key: "sdf-25", Expression: @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                          => @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.id.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()",

                // sdf-29, syntax error, 'specialization' and 'derivation' are reversed
                { Key: "sdf-29", Expression: @"((kind in 'resource' | 'complex-type') and (specialization = 'derivation')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()" }
                                          => @"((kind in 'resource' | 'complex-type') and (derivation= 'specialization')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()",

                _ => constraintElement.Expression
            };
        }
    }
}