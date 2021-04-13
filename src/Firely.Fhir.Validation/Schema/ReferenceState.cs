/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System.Collections.Concurrent;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class ReferenceState
    {
        /// <summary>
        /// All references that the validator visisted
        /// </summary>
        private readonly ConcurrentBag<ReferenceId> _referencesVisited = new();

        public void AddReference(string location, string reference, string? targetProfile = null)
        {
            _referencesVisited.Add(new(location, reference, targetProfile));
        }

        public bool Visited(string location, string reference, string? targetProfile = null)
            => _referencesVisited.Contains(new(location, reference, targetProfile));

        private record ReferenceId(string Location, string Reference, string? TargetProfile = null);
    }
}
