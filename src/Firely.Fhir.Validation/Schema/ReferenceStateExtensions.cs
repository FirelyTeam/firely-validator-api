/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    //internal static class ReferenceStateExtensions
    //{
    //    /// <summary>
    //    /// Creates a new <see cref="ValidationState"/> with the given reference added to 
    //    /// the <see cref="ReferenceState"/> in the original ValidationState
    //    /// </summary>
    //    /// <param name="state">The current ValidationState</param>
    //    /// <param name="reference">the reference uri of the instance</param>
    //    public static ValidationState AddReferenceState(this ValidationState state, string reference)
    //    {
    //        ReferenceState referenceState = state.GetStateItem<ReferenceState>();
    //        var newState = referenceState.WithNewReference(reference);
    //        return state.WithStateItem(newState);
    //    }

    //    /// <summary>
    //    /// Checks whether we already visisted the instance with <paramref name="reference"/> or not.
    //    /// </summary>
    //    /// <param name="state">The current ValidationState</param>
    //    /// <param name="reference">the reference uri of the instance</param>
    //    /// <returns><see langword="true"/> if the instance already have been visited; otherwise, <see langword="false"/>.</returns>
    //    public static bool Visited(this ValidationState state, string reference)
    //        => state.GetStateItem<ReferenceState>().Visited(reference);

    //}
}