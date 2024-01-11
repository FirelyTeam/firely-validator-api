/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An implementation of <see cref="IExternalReferenceResolver"/> that uses an in-memory dictionary.
    /// </summary>
    public class InMemoryExternalReferenceResolver : Dictionary<string, object>, IExternalReferenceResolver
    {
        /// <inheritdoc/>
        public Task<object?> ResolveAsync(string reference) =>
            Task.FromResult(TryGetValue(reference, out var resource) ? resource : null);
    }
}