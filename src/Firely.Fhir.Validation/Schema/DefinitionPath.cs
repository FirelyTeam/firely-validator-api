/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A class representing a reference to a definition of an element. Aspects of the path are the 
    /// full series of profiles invoked, the element names and slices navigated into.
    /// This class is also used to track the location of an instance.
    /// </summary>
    /// <example>An example skeleton path is: "Resource(canonical).childA.childB[sliceB1].childC -> Datatype(canonical).childA"</example>
    internal class DefinitionPath : PathStack
    {
        private DefinitionPath(PathStackEvent? current) : base(current)
        {
        }

        /// <summary>
        /// Whether the path contains information that cannot be derived from the instance path.
        /// </summary>
        public bool HasDefinitionChoiceInformation
        {
            get
            {
                var scan = Current;

                while (scan is not null)
                {
                    if (scan is InvokeProfileEvent s && s.IsProfiledFhirType) return true;
                    if (scan is CheckSliceEvent) return true;
                    scan = scan.Previous;
                }

                return false;
            }
        }

        internal bool MatchesContext(string contextEid)
        {
            var match = new Regex("^(?<type>[A-Za-z]*).?(?<location>.*)$").Match(contextEid);
            
            if (!match.Success)
            {
                throw new InvalidOperationException($"Invalid context element id: {contextEid}");
            }
            
            var type = match.Groups["type"].Value;
            var location = match.Groups["location"].Value;
            var locationComponents = location == "" ? [] : location.Split('.', ':');

            return matchesContext(
                findExtensionContext(Current),
                type, 
                locationComponents
            );
            
            static PathStackEvent? findExtensionContext(PathStackEvent? current) => 
                current switch 
                {
                    null => null,
                    ChildNavEvent { ChildName: "extension" or "modifierExtension" } cne => current.Previous,
                    _ => findExtensionContext(current.Previous)
                };
        }
        
        private static bool matchesContext(PathStackEvent? current, string type, string[] location)
        {
            if (location.Length == 0)
                return current switch
                {
                    ChildNavEvent cne => cne.Type == type || type == "Element",
                    CheckSliceEvent cse => cse.Type == type || $"{cse.Type}:{cse.SliceName}" == type || type == "Element",
#pragma warning disable CS0618 // Type or member is obsolete
                    InvokeProfileEvent ipe => ((FhirSchema)ipe.Schema).GetOwnAndBaseTypes().Contains(type),
#pragma warning restore CS0618 // Type or member is obsolete
                    _ => false
                };

            return current switch
            {
                ChildNavEvent cne => cne.ChildName == location.Last() && matchesContext(current.Previous, type, location[..^1]),
                CheckSliceEvent cse => cse.SliceName == location.Last() && matchesContext(current.Previous, type, location[..^1]) || matchesContext(current.Previous, type, location),
                InvokeProfileEvent => matchesContext(current.Previous, type, location),
                _ => false
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Current is not null ? render(Current) : string.Empty;

            static string render(PathStackEvent e) =>
                e.Previous is not null
                    ? combine(render(e.Previous), e)
                    : e.Render();

            static string combine(string left, PathStackEvent right) =>
                right is InvokeProfileEvent ipe ? left + "->" + ipe.Render() : left + right.Render();
        }


        /// <summary>
        /// Returns whether the path contains slice information, and if so, returns the slice information in the sliceInfo out parameter.
        /// </summary>
        /// <param name="sliceInfo">Slice information.</param>
        /// <returns> Whether the path contains slice information.</returns>
        public bool TryGetSliceInfo(out string? sliceInfo)
        {
            var scan = Current;
            sliceInfo = null;

            while (scan is not null)
            {
                if (scan is CheckSliceEvent cse)
                {
                    sliceInfo = sliceInfo == null ? cse.SliceName : $"{cse.SliceName}, subslice {sliceInfo}";
                }
                else if (sliceInfo != null)
                {
                    return true;
                }

                scan = scan.Previous;
            }

            return false;
        }

        /// <summary>
        /// Start a new DefinitionPath.
        /// </summary>
        public static DefinitionPath Start() => new(null);

        /// <summary>
        /// Update the path to include a move to a child element.
        /// </summary>
        public DefinitionPath ToChild(string name, string? type = null) => new(new ChildNavEvent(Current, name, null, type));

        /// <summary>
        /// Update the path to include an invocation of a (nested) profile.
        /// </summary>
        public DefinitionPath InvokeSchema(ElementSchema schema) => new(new InvokeProfileEvent(Current, schema));

        /// <summary>
        /// Update the path to include a move into a specific slice.
        /// </summary>
        public DefinitionPath CheckSlice(string sliceName, string type) => new(new CheckSliceEvent(Current, sliceName, type));
    }
}