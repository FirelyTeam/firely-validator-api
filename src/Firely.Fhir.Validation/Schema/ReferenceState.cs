/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class ReferenceState
    {
        /// <summary>
        /// All references that the validator visited
        /// </summary>
        private string[] _referencesVisited = Array.Empty<string>();

        public ReferenceState WithNewReference(string reference)
        {
            var newReferencesVisited = new string[_referencesVisited.Length + 1];
            _referencesVisited.CopyTo(newReferencesVisited, 0);
            newReferencesVisited[_referencesVisited.Length] = reference;

            return new ReferenceState
            {
                _referencesVisited = newReferencesVisited
            };

        }

        public bool Visited(string reference) => _referencesVisited.Contains(reference);
    }
}
