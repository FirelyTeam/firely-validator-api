/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Utility;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// How resource references can be aggregated.
    /// (url: http://hl7.org/fhir/ValueSet/resource-aggregation-mode)
    /// (system: http://hl7.org/fhir/resource-aggregation-mode)
    /// </summary>
    [FhirEnumeration("AggregationMode")]
    internal enum AggregationMode
    {
        /// <summary>
        /// The reference is a local reference to a contained resource.
        /// (system: http://hl7.org/fhir/resource-aggregation-mode)
        /// </summary>
        [EnumLiteral("contained", "http://hl7.org/fhir/resource-aggregation-mode"), Description("Contained")]
        Contained,
        /// <summary>
        /// The reference to a resource that has to be resolved externally to the resource that includes the reference.
        /// (system: http://hl7.org/fhir/resource-aggregation-mode)
        /// </summary>
        [EnumLiteral("referenced", "http://hl7.org/fhir/resource-aggregation-mode"), Description("Referenced")]
        Referenced,
        /// <summary>
        /// The resource the reference points to will be found in the same bundle as the resource that includes the reference.
        /// (system: http://hl7.org/fhir/resource-aggregation-mode)
        /// </summary>
        [EnumLiteral("bundled", "http://hl7.org/fhir/resource-aggregation-mode"), Description("Bundled")]
        Bundled,
    }
}
