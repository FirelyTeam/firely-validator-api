/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Validation.Compilation.Shared.Correctors;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using System;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Helper methods for structure definition to apply corrections according to the R3/R4 FHIR specification and
    /// applies them to the resolved StructureDefinitions. 
    /// </summary>
    /// <remarks>
    /// This class is marked public since it is useful across the Firely products and 
    /// we recommend only using it if you are aware of the kind of corrections done by this helper class.
    /// </remarks>
    public static class StructureDefinitionCorrectionExtensions
    {
        /// <summary>
        /// Apply corrections to the specified resource.
        /// </summary>
        /// <param name="result">The corrected resource.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Resource? Correct(this Resource? result)
        {
            // If this is not a StructureDefinition, just pass it on without doing anything to it.
            if (result is not StructureDefinition sd) return result;

            var fhirRelease = getFhirRelease(sd);

            CorrectorFactory.Get(sd.Kind)?.Correct(fhirRelease, sd);
            CorrectorFactory.Get(sd.Type)?.Correct(fhirRelease, sd);

            return sd;
        }

        private static FhirRelease? getFhirRelease(StructureDefinition sd)
        {
#if R4_AND_LATER
            var fhirVersion = sd.FhirVersion?.GetLiteral();

            if (!FhirReleaseParser.TryParse(fhirVersion, out var fhirRelease))
                fhirRelease = null;

            return fhirRelease;
#else
            return FhirRelease.STU3;
#endif
        }
    }
}
