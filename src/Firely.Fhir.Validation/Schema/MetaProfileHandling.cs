/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Type of action to perform at a meta profile.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Accept the meta profile and optionally return another profile.
        /// </summary>
        Accept,
        /// <summary>
        /// Decline the meta profile.
        /// </summary>
        Decline
    }

    /// <summary>
    /// The result of the callback <see cref="ValidationContext.FollowMetaProfile" />.
    /// </summary>
    /// <param name="Action">What to do with the meta profile.</param>
    /// <param name="NewProfile">Optional return a new profile.</param>
    public record MetaProfileHandling(ActionType Action, Canonical? NewProfile = null);
}
