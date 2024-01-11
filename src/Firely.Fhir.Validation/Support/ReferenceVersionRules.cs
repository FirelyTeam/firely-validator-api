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
    /// Whether a reference needs to be version specific or version independent, or whether either can be used
    /// (url: http://hl7.org/fhir/ValueSet/reference-version-rules)
    /// (system: http://hl7.org/fhir/reference-version-rules)
    /// </summary>
    [FhirEnumeration("ReferenceVersionRules")]
    internal enum ReferenceVersionRules
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
