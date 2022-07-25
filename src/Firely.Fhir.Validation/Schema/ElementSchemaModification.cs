/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Extension methods to
    /// </summary>
    public static class ElementSchemaModification
    {
        /// <summary>
        /// Creates a new <see cref="ElementSchema"/> with the given members added to its original.
        /// </summary>
        public static ElementSchema WithMembers(this ElementSchema original, IEnumerable<IAssertion> additional) =>
            original.CloneWith(original.Id, original.Members.Concat(additional));

        /// <inheritdoc cref="WithMembers(ElementSchema, IAssertion[])" />
        public static ElementSchema WithMembers(this ElementSchema original, params IAssertion[] additional)
            => original.WithMembers(additional.AsEnumerable());

        /// <summary>
        /// Creates a new <see cref="ElementSchema"/> with the same members as the original, but a different id.
        /// </summary>
        public static ElementSchema WithId(this ElementSchema original, Canonical id) =>
            original.CloneWith(id, original.Members);
    }
}
