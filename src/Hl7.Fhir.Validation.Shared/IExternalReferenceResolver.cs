/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */
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
        /// Resolves the reference to a resource.
        /// </summary>
        /// <returns>The resource, or null if the reference could not be resolved.</returns>
        Task<Resource?> ResolveAsync(string reference);
    }

}
