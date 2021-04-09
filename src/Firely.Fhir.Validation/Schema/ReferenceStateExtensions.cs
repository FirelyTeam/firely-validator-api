/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

namespace Firely.Fhir.Validation
{
    public static class ReferenceStateExtensions
    {
        /// <summary>
        /// Adds the reference state to the ValidationState
        /// </summary>
        /// <param name="state">The current ValidationState</param>
        /// <param name="location">location of the instance</param>
        /// <param name="reference">the reference uri of the instance</param>
        /// <param name="targetProfile">not used yet</param>
        public static ValidationState AddReferenceState(this ValidationState state, string location, string reference, string? targetProfile = null)
        {
            ReferenceState referenceState = state.GetStateItem<ReferenceState>();

            referenceState.AddReference(location, reference, targetProfile);
            return state;
        }

        /// <summary>
        /// Checks whether we already visisted the instance with <paramref name="location"/> and <paramref name="reference"/> or not 
        /// </summary>
        /// <param name="state">The current ValidationState</param>
        /// <param name="location">location of the instance</param>
        /// <param name="reference">the reference uri of the instance</param>
        /// <param name="targetProfile">not used yet</param>
        /// <returns><see langword="true"/> if the instance already have been visited; otherwise, <see langword="false"/>.</returns>
        public static bool Visited(this ValidationState state, string location, string reference, string? targetProfile = null)
            => state.GetStateItem<ReferenceState>().Visited(location, reference, targetProfile);

    }
}