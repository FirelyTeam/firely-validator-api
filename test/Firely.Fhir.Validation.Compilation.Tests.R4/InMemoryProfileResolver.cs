/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using System.Linq;
using Tasks = System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class InMemoryProfileResolver : IResourceResolver, IConformanceSource, IAsyncResourceResolver
    {
        ILookup<string, Resource> _resources = Enumerable.Empty<Resource>().ToLookup(r => "", r => r);

        public InMemoryProfileResolver(IEnumerable<IConformanceResource> profiles)
        {
            Reload(profiles);
        }

        public InMemoryProfileResolver(params IConformanceResource[] profiles) : this(profiles.AsEnumerable()) { }

        public InMemoryProfileResolver(IConformanceResource profile) : this(new IConformanceResource[] { profile }) { }

        public void Reload(IEnumerable<IConformanceResource> profiles)
        {
            _resources = profiles.Where(p => p is Resource).ToLookup(r => r.Url, r => (Resource)r);
        }

        public void Reload(IConformanceResource[] profiles) => Reload(profiles.AsEnumerable());

        public void Reload(IConformanceResource profile) => Reload(new IConformanceResource[] { profile });

        public void Clear() => Reload(Enumerable.Empty<IConformanceResource>());

        #region IResourceResolver

        public Resource? ResolveByCanonicalUri(string uri) => _resources[uri].FirstOrDefault();
        public Resource? ResolveByUri(string uri) => null;

        #endregion

        #region IAsyncResourceResolver

        public Tasks.Task<Resource?> ResolveByUriAsync(string uri) => Tasks.Task.FromResult<Resource?>(null);
        public Tasks.Task<Resource?> ResolveByCanonicalUriAsync(string uri) => Tasks.Task.FromResult(ResolveByCanonicalUri(uri));

        #endregion

        #region IConformanceSource

        public CodeSystem? FindCodeSystemByValueSet(string valueSetUri)
            => throw new System.NotImplementedException();

        public IEnumerable<ConceptMap> FindConceptMaps(string? sourceUri = null, string? targetUri = null)
            => throw new System.NotImplementedException();

        public NamingSystem? FindNamingSystem(string uniqueId)
            => throw new System.NotImplementedException();

        public IEnumerable<string> ListResourceUris(ResourceType? filter = default)
            => _resources.Select(g => g.Key);
        #endregion
    }
}