/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System.Linq;

namespace Firely.Reflection.Emit
{
    // This class should really be transferred to the SDK
    internal static class ISourceNodeExtensions
    {
        /// <summary>
        /// Returns first child with the specified <paramref name="name"/>, if any.
        /// </summary>
        public static ISourceNode? Child(this ISourceNode element, string name, int arrayIndex = 0) =>
            element?.Children(name)?.Skip(arrayIndex).FirstOrDefault();

        /// <summary>
        /// Returns the value of the first child with the specified <paramref name="name"/> as string, if any.
        /// </summary>
        public static string? ChildString(this ISourceNode element, string name, int arrayIndex = 0) =>
            element.Child(name, arrayIndex)?.Text;

        public static ISourceNode? GetExtension(this ISourceNode sourceNode, string system) =>
            sourceNode?.Children("extension")?.FirstOrDefault(c => c.ChildString("url") == system);

        public static string? GetStringExtension(this ISourceNode sourceNode, string system)
        {
            var extensionNode = sourceNode?.GetExtension(system);
            return extensionNode?.ChildString("valueString");
        }
    }
}
