/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A service that can resolve external references to other resources.
    /// </summary>
    public interface IExternalReferenceResolver
    {
        /// <summary>
        /// Resolves the reference to a resource. The returned object must be either a <see cref="Resource"/> or <see cref="ElementNode"/>
        /// </summary>
        /// <returns>The resource or element node, or null if the reference could not be resolved.</returns>
        Task<object?> ResolveAsync(string reference);
    }

}
