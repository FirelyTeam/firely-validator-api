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
using System.Collections.Generic;
using System.Linq;

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
#if R4_AND_LATER
        private static readonly Dictionary<FHIRVersion, FhirRelease> FHIR_VERSION_TO_RELEASE = new();

        /// <summary>
        /// Because GetLiteral and FhirReleaseParser.TryParse are relatively expensive operations
        /// we create a lookup table from FHIRVersion to FhirRelease.
        /// </summary>
        static StructureDefinitionCorrectionExtensions()
        {
            foreach (var fhirVersion in Enum.GetValues(typeof(FHIRVersion)).Cast<FHIRVersion>())
            {
                var literal = fhirVersion.GetLiteral();

                if (FhirReleaseParser.TryParse(literal, out var fhirRelease))
                    FHIR_VERSION_TO_RELEASE.Add(fhirVersion, fhirRelease.Value);
            }
        }
#endif

        /// <summary>
        /// Apply corrections to the specified resource.
        /// </summary>
        /// <param name="resource">The uncorrected resource.</param>
        public static void Correct(this Resource? resource)
        {
            // If this is not a StructureDefinition, just pass it on without doing anything to it.
            if (resource is not StructureDefinition sd) 
                return;

            var fhirRelease = getFhirRelease(sd);

            CorrectorFactory.Get(sd.Kind)?.Correct(fhirRelease, sd);
            CorrectorFactory.Get(sd.Type)?.Correct(fhirRelease, sd);
        }

        /// <summary>
        /// Apply corrections to the specified resource (fluent interface pattern)
        /// </summary>
        /// <param name="resource">The uncorrected resource.</param>
        /// <returns>Returns the corrected resource</returns>
        public static Resource? WithCorrections(this Resource? resource)
        {
            resource.Correct();
            return resource;
        }

        private static FhirRelease? getFhirRelease(StructureDefinition sd)
        {
#if R4_AND_LATER
            return sd.FhirVersion.HasValue
                ? FHIR_VERSION_TO_RELEASE.GetValueOrDefault(sd.FhirVersion.Value)
                : null;
#else
            return FhirRelease.STU3;
#endif
        }
    }
}
