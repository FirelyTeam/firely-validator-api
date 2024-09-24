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

        internal IEnumerable<string> GetAllPossibleElementIds()
        {
            // if the previous event was not an invoke profile event, we need to explicitly include base and element (it cannot be a resource) or a complex element
            var needsElementAdded = Current?.Previous is not InvokeProfileEvent;
            return getPossibleElementIds(Current).Concat(needsElementAdded ? ["Base.extension", "Element.extension"] : []);

            IEnumerable<string> getPossibleElementIds(PathStackEvent? e, string current = "")
            {
                return e switch
                {
                    null => [current],
                    ChildNavEvent cne => getPossibleElementIds(cne.Previous, $".{cne.ChildName}{current}"),
                    CheckSliceEvent cse => getPossibleElementIds(cse.Previous, $":{cse.SliceName}{current}").Concat(getPossibleElementIds(cse.Previous, current)),
                    InvokeProfileEvent ipe =>
                        (ipe.Previous is not null 
                            ? getPossibleElementIds(ipe.Previous, current) 
                            : [])
                                .Concat
                                (
#pragma warning disable CS0618 // Type or member is obsolete
                                    getOwnAndBaseTypesFromSchema((FhirSchema)ipe.Schema).Select(type => $"{type}{current}")
#pragma warning restore CS0618 // Type or member is obsolete
                                ),
                    _ => throw new InvalidOperationException("Unexpected event type")
                };
            }
            
#pragma warning disable CS0618 // Type or member is obsolete
            IEnumerable<string> getOwnAndBaseTypesFromSchema(FhirSchema schema)
            {
#pragma warning restore CS0618 // Type or member is obsolete
                return (schema.StructureDefinition.BaseCanonicals ?? Enumerable.Empty<Canonical>())
                    .Append(schema.StructureDefinition.Canonical)
                    .Select(canonical => canonical.Uri!.Split('/').Last());
            }
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
        public DefinitionPath ToChild(string name) => new(new ChildNavEvent(Current, name, null));

        /// <summary>
        /// Update the path to include an invocation of a (nested) profile.
        /// </summary>
        public DefinitionPath InvokeSchema(ElementSchema schema) => new(new InvokeProfileEvent(Current, schema));

        /// <summary>
        /// Update the path to include a move into a specific slice.
        /// </summary>
        public DefinitionPath CheckSlice(string sliceName) => new(new CheckSliceEvent(Current, sliceName));
    }
}