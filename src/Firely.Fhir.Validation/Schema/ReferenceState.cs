/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
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

        internal void AddReference(string location, string reference, string? targetProfile = null)
        {
            _referencesVisited.Add(new(location, reference, targetProfile));
        }

        internal bool Visited(string location, string reference, string? targetProfile = null)
            => _referencesVisited.Contains(new(location, reference, targetProfile));

        internal record ReferenceId(string Location, string Reference, string? TargetProfile = null);
    }
}
