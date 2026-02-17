#if R4_AND_LATER

using Hl7.Fhir.Specification;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class OperationDefinitionCorrector : ConstraintsCorrector
{
    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        switch (fhirRelease)
        {
            case FhirRelease.R4 or FhirRelease.R4B:
                correctR4OrR4BConstraints(constraintElements);
                break;

            case FhirRelease.R5:
                correctR5Constraints(constraintElements);
                break;
        }
    }

    private static void correctR4OrR4BConstraints(IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                // correct opd-3:
                { Key: "opd-3", Expression: @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical')", }
                                         => @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))",

                _ => constraintElement.Expression
            };
        }
    }

    private static void correctR5Constraints(IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                // correct opd-3:
                { Key: "opd-3", Expression: @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))" }
                                         => @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/all-resource-types'))",

                _ => constraintElement.Expression
            };
        }
    }
}

#endif