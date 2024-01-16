/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;
using static Hl7.Fhir.Model.ElementDefinition;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="FhirPathValidator"/>.
    /// </summary>
    internal class FhirPathBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is part of an element (whether referring to a backbone type or not),
            // so this should not be part of the type generated for a backbone (see eld-5).
            // Note: the snapgen will ensure this constraint is copied over from the referred
            // element to the referring element (= which has a contentReference).
            if (conversionMode == ElementConversionMode.BackboneType) yield break;

            foreach (var constraint in nav.Current.Constraint)
            {
                if (getBuiltInValidatorFor(constraint.Key) is { } biv)
                    yield return biv;
                else
                {
                    var bestPractice = constraint.GetBoolExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice") ?? false;
                    var fpAssertion = new FhirPathValidator(constraint.Key, constraint.Expression, constraint.Human, convertConstraintSeverity(constraint.Severity), bestPractice);
                    yield return fpAssertion;
                }
            }

            static IssueSeverity? convertConstraintSeverity(ConstraintSeverity? constraintSeverity) => constraintSeverity switch
            {
                ConstraintSeverity.Error => IssueSeverity.Error,
                ConstraintSeverity.Warning => IssueSeverity.Warning,
                _ => default,
            };
        }

        private static InvariantValidator? getBuiltInValidatorFor(string key) => key switch
        {
            "ele-1" => new FhirEle1Validator(),
            "ext-1" => new FhirExt1Validator(),
            "txt-1" => new FhirTxt1Validator(),
            "txt-2" => new FhirTxt2Validator(),
            _ => null
        };
    }
}
