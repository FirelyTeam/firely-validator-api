/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

#nullable enable


using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal static class IScopedNodeExtensions
    {
        /// <summary>
        /// Converts a <see cref="IScopedNode"/> to a <see cref="ITypedElement"/>.
        /// </summary>
        /// <param name="node">An <see cref="IScopedNode"/> node</param>
        /// <returns>An implementation of <see cref="ITypedElement"/></returns>
        /// <remarks>Be careful when using this method, the returned <see cref="ITypedElement"/> does not implement
        /// the methods <see cref="ITypedElement.Location"/> and <see cref="ITypedElement.Definition"/>.    
        /// </remarks>
        [Obsolete("WARNING! For internal API use only. Turning an IScopedNode into an ITypedElement will cause problems for" +
            "Location and Definitions. Those properties are not implemented using this method and can cause problems " +
            "elsewhere. Please don't use this method unless you know what you are doing.")]
        public static ITypedElement AsTypedElement(this IScopedNode node) => node switch
        {
            TypedElementToIScopedNodeToAdapter adapter => adapter.ScopedNode,
            ITypedElement ite => ite,
            _ => new ScopedNodeToTypedElementAdapter(node)
        };
        //node is ITypedElement ite ? ite : new ScopedNodeToTypedElementAdapter(node);

        public static ScopedNode ToScopedNode(this IScopedNode node) => node switch
        {
            TypedElementToIScopedNodeToAdapter adapter => adapter.ScopedNode,
            _ => throw new ArgumentException("The node is not a TypedElementToIScopedNodeToAdapter")
        };

        /// <summary>
        /// Returns the parent resource of this node, or null if this node is not part of a resource.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<IScopedNode> Children(this IEnumerable<IScopedNode> nodes, string? name = null) =>
           nodes.SelectMany(n => n.Children(name));

        internal static bool Matches(this IScopedNode value, ITypedElement pattern)
        {
            if (value == null && pattern == null) return true;
            if (value == null || pattern == null) return false;

            if (!TypedElementExtensions.ValueEquality(value.Value, pattern.Value)) return false;

            // Compare the children.
            var valueChildren = value.Children();
            var patternChildren = pattern.Children();

            return patternChildren.All(patternChild => valueChildren.Any(valueChild =>
                patternChild.Name == valueChild.Name && valueChild.Matches(patternChild)));

        }


        internal static bool IsExactlyEqualTo(this IScopedNode left, ITypedElement right, bool ignoreOrder = false)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;

            if (!TypedElementExtensions.ValueEquality(left.Value, right.Value)) return false;

            // Compare the children.
            var childrenL = left.Children();
            var childrenR = right.Children();

            if (childrenL.Count() != childrenR.Count())
                return false;

            if (ignoreOrder)
            {
                childrenL = childrenL.OrderBy(x => x.Name).ToList();
                childrenR = childrenR.OrderBy(x => x.Name).ToList();
            }

            return childrenL.Zip(childrenR,
                (childL, childR) => childL.Name == childR.Name && childL.IsExactlyEqualTo(childR, ignoreOrder)).All(t => t);
        }

        internal static string? ComputeReferenceCycle(this IScopedNode current, ValidationSettings vc) =>
            current.ToScopedNode().ComputeReferenceCycle(vc);
        
        internal static string? ComputeReferenceCycle(this ScopedNode current, ValidationSettings vc, IList<(string, string)>? followed = null) // this is expensive, but only executed when a loop is detected. We accept this
        {
            
            if (current.AtResource && followed?.Count(c => c.Item2 == current.Location) is 2) // if we followed the same reference twice, we have a loop
            {
                return string.Join(" | ", followed.Select(reference => $"{reference.Item1} -> {reference.Item2}"));
            }

            followed ??= [];

            foreach (var child in current.Children())
            {
                var childNode = child.ToScopedNode();
                
                if (childNode.InstanceType == "Reference") 
                {
                    var target = childNode.Resolve(url => vc.ResolveExternalReference is { } resolve ? resolve(url, childNode.Location) : null)?.ToScopedNode();
                    if (target is null || (childNode.Location.StartsWith(target.Location + ".contained["))) // external reference, or reference to parent container: we do not include these in the cycle
                    {
                        continue;
                    }
                    
                    followed.Add((childNode.Location, target.Location)); // add the reference to the list of followed references
                    
                    if(ComputeReferenceCycle(target, vc, followed) is { } result) 
                        return result; // if multiple paths are found, we only return the first one. Rerunning will show the next one. Let's hope that never happens.
                }

                if (ComputeReferenceCycle(childNode, vc, followed) is { } result2) // why is result still in scope? that makes no sense
                {
                    return result2;
                }
            }

            return null;
        }
    }
}

#nullable restore