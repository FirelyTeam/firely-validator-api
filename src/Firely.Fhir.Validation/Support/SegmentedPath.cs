/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System.Collections.Generic;
using System.Globalization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A single segment of a dotted element path: the element name, plus the optional repetition
    /// index (<c>name[3]</c>, as found in instance locations) or slice name (<c>name:slice</c>,
    /// as found in definition element ids).
    /// </summary>
    internal readonly record struct PathSegment(string Name, int? Index, string? SliceName);

    /// <summary>
    /// Tokenizes dotted element paths into <see cref="PathSegment"/>s. This is the common ground
    /// between the instance locations produced by <c>PocoNode.GetLocation()</c>
    /// (e.g. <c>Bundle.entry[0].resource[0].name[1]</c>) and the definition paths/element ids used in
    /// profiles (e.g. <c>Observation.category:vscat.coding</c>).
    /// </summary>
    internal static class SegmentedPath
    {
        /// <summary>
        /// Splits <paramref name="path"/> on <c>.</c> and parses each segment's optional
        /// <c>[index]</c> and <c>:slicename</c> decorations.
        /// </summary>
        public static IReadOnlyList<PathSegment> Tokenize(string path)
        {
            var segments = path.Split('.');
            var result = new List<PathSegment>(segments.Length);

            foreach (var segment in segments)
            {
                var name = segment;
                int? index = null;
                string? sliceName = null;

                // Only a bracketed integer is an index: choice-type names like "value[x]" stay intact.
                var bracket = name.IndexOf('[');
                if (bracket >= 0 && name.EndsWith("]", System.StringComparison.Ordinal)
                    && int.TryParse(name[(bracket + 1)..^1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                {
                    index = parsed;
                    name = name[..bracket];
                }

                var colon = name.IndexOf(':');
                if (colon >= 0)
                {
                    sliceName = name[(colon + 1)..];
                    name = name[..colon];
                }

                result.Add(new PathSegment(name, index, sliceName));
            }

            return result;
        }
    }
}
