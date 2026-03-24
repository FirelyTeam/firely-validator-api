#if R4_AND_LATER

using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class StructureDefinitionCorrector : ConstraintsCorrector
{
    public StructureDefinitionCorrector() : base("http://hl7.org/fhir/StructureDefinition/StructureDefinition")
    {
        RegisterInvalidConstraint("StructureDefinition", 
                                  "sdf-0",
                                  "name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                                  "name.exists() implies name.matches('^[A-Z]([A-Za-z0-9_]){0,254}$')",
                                  FhirRelease.R4);
        RegisterInvalidConstraint("StructureDefinition", 
                                  "sdf-0",
                                  "name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                                  "name.exists() implies name.matches('^[A-Z]([A-Za-z0-9_]){0,254}$')",
                                  FhirRelease.R4B);

        // do not use $this (see https://jira.hl7.org/browse/FHIR-37761)
        RegisterInvalidConstraint("StructureDefinition.snapshot", 
                                  "sdf-24",
                                  "element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                                  "element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.id.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                                  FhirRelease.R4B);
        RegisterInvalidConstraint("StructureDefinition.snapshot",
                                  "sdf-25",
                                  "element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                                  "element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.id.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                                  FhirRelease.R4B);

        // sdf-29, syntax error, 'specialization' and 'derivation' are reversed
        RegisterInvalidConstraint("StructureDefinition",
                                  "sdf-29",
                                  "((kind in 'resource' | 'complex-type') and (specialization = 'derivation')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()",
                                  "((kind in 'resource' | 'complex-type') and (derivation = 'specialization')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()",
                                  FhirRelease.R5);
    }
}

#endif