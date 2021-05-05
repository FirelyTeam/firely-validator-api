/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Utility;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Whether a reference needs to be version specific or version independent, or whether either can be used
    /// (url: http://hl7.org/fhir/ValueSet/reference-version-rules)
    /// (system: http://hl7.org/fhir/reference-version-rules)
    /// </summary>
    [FhirEnumeration("ReferenceVersionRules")]
    public enum ReferenceVersionRules
    {
        /// <summary>
        /// The reference may be either version independent or version specific
        /// (system: http://hl7.org/fhir/reference-version-rules)
        /// </summary>
        [EnumLiteral("either", "http://hl7.org/fhir/reference-version-rules"), Description("Either Specific or independent")]
        Either,
        /// <summary>
        /// The reference must be version independent
        /// (system: http://hl7.org/fhir/reference-version-rules)
        /// </summary>
        [EnumLiteral("independent", "http://hl7.org/fhir/reference-version-rules"), Description("Version independent")]
        Independent,
        /// <summary>
        /// The reference must be version specific
        /// (system: http://hl7.org/fhir/reference-version-rules)
        /// </summary>
        [EnumLiteral("specific", "http://hl7.org/fhir/reference-version-rules"), Description("Version Specific")]
        Specific,
    }


}
