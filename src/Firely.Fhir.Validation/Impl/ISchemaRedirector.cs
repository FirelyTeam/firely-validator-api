/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// This interface can be implemented by schema's that need to start additional validations. This happens for example
    /// in <see cref="ExtensionSchema"/> and <see cref="ResourceSchema"/></summary>, which will obtain additional
    /// references from e.g. Meta.profile and Extension.url.
    public interface ISchemaRedirector
    {
        /// <summary>
        /// Given an instance, retrieve additional schema's to validate against.
        /// </summary>
        Canonical[] GetRedirects(ITypedElement instance);
    }
}
