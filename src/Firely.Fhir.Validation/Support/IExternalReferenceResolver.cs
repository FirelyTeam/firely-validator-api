/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
