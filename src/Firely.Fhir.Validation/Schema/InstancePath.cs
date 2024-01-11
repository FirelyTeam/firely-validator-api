/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A class representing the location of an instance.
    /// </summary>
    internal class InstancePath : PathStack
    {
        private InstancePath(PathStackEvent? current) : base(current)
        {
        }

        /// <summary>
        /// Render the path as a string that can be used to obtain the location of an instance.
        /// </summary>
        /// <returns>Location of the instance</returns>
        public override string ToString()
        {
            return render(Current);

            static string render(PathStackEvent? e) => e switch
            {
                null => string.Empty,
                InternalReferenceNavEvent ire => ire.Render(), // not render the previous elements anymore
                { Previous: not null } => $"{render(e.Previous)}{e.Render()}", // the normal recursive
                _ => e.Render()
            };
        }

        /// <summary>
        /// Start a new InstancePath.
        /// </summary>
        public static InstancePath Start() => new(null);

        /// <summary>
        /// Start the instance path with a reference to a resource. Can only be used at the beginning of the path.
        /// </summary>
        /// <param name="resource">The resource type.</param>
        /// <returns></returns>
        internal InstancePath StartResource(string resource) =>
            Current is null
                ? new(new StartResourceNavEvent(Current, resource)) // Can only start at the beginning of the path.
                : this;

        /// <summary>
        /// Update the path to include a move to a child element.
        /// </summary>
        /// <param name="name">The name of the child element.</param>
        /// <param name="choiceType">The type of the choice element, if applicable.</param>
        public InstancePath ToChild(string name, string? choiceType = null) => new(new ChildNavEvent(Current, name, choiceType));

        /// <summary>
        /// Update the path to include an index of a child element.
        /// </summary>
        /// <param name="index">The index of the child element.</param>
        public InstancePath ToIndex(int index) =>
            Current is not null
            ? new(new IndexNavEvent(Current, index)) // Cannot start at the beginning of the path.
            : this;

        /// <summary>
        /// Update the path to include a set of indices of a group element, which can be used later with a <see cref="IndexNavEvent"/>.
        /// </summary>
        /// <param name="indices">The indices of the group element.</param>
        public InstancePath AddOriginalIndices(IEnumerable<int> indices) =>
            indices.Any()
                ? new(new OriginalIndicesNavEvent(Current, indices))
                : this;

        /// <summary>
        /// Update the path to include a reference to an internal element.
        /// </summary>
        public InstancePath AddInternalReference(string location) => new(new InternalReferenceNavEvent(Current, location));
    }
}