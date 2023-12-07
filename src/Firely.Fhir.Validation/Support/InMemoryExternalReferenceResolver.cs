/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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