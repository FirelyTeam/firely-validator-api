/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */


using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class InMemoryExternalReferenceResolver : Dictionary<string, Resource>, IExternalReferenceResolver
    {
        public Task<object?> ResolveAsync(string reference) =>
            System.Threading.Tasks.Task.FromResult(this.TryGetValue(reference, out var resource) ? (object)resource : null);
    }
}