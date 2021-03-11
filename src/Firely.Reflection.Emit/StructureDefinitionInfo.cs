/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Reflection.Emit
{
    internal record StructureDefinitionInfo(string Canonical, string TypeName, bool IsAbstract, bool IsResource, string? BaseCanonical, ElementDefinitionInfo[] Elements)
    {
        public static StructureDefinitionInfo FromStructureDefinition(ISourceNode structureDefNode)
        {
            var canonical = structureDefNode.ChildString("url") ?? throw new InvalidOperationException("Missing 'url' in the StructureDefinition.");
            var typeName = structureDefNode.ChildString("name") ?? throw new InvalidOperationException("Missing 'name' in the StructureDefinition.");
            var baseDefinition = structureDefNode.ChildString("baseDefinition");
            var isAbstract = "true" == structureDefNode.ChildString("abstract");
            var isResource = "resource" == structureDefNode.ChildString("kind");

            var elementNodes = structureDefNode.Child("differential")?.Children("element")?.Skip(1).ToArray()
                        ?? Array.Empty<ISourceNode>();

            var elements = fromSourceNodes(canonical, typeName, elementNodes).ToArray();

            return new StructureDefinitionInfo(canonical, typeName, isAbstract, isResource, baseDefinition, elements);
        }

        public static StructureDefinitionInfo FromBackbone(string elementName, string sdCanonical, string sdTypeName, string backboneType, ArraySegment<ISourceNode> backboneNode)
        {
            if (!backboneNode.Any()) throw new ArgumentException("Cannot read a backbone from an empty list of ISourceNodes.");
            ISourceNode backboneRoot = backboneNode[0]!;

            var backboneTypeName = sdTypeName + "#" + (getExplicitTypeName() ?? pascalBackboneName());
            var backboneCanonical = sdCanonical + "#" + backboneRoot.ChildString("path");

            var elements = fromSourceNodes(sdCanonical, sdTypeName, backboneNode[1..]).ToArray();
            return new StructureDefinitionInfo(backboneCanonical, backboneTypeName, IsAbstract: false, IsResource: false, backboneType, elements);

            string? getExplicitTypeName() => backboneRoot.GetExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-explicit-type-name")?
                .ChildString("valueString");
            string pascalBackboneName() => char.ToUpperInvariant(elementName[0]) + elementName[1..];
        }

        private static IEnumerable<ElementDefinitionInfo> fromSourceNodes(string sdCanonical, string sdTypeName, ArraySegment<ISourceNode> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) yield break;

            var current = 0;
            var initialLength = pathLength();

            do
            {
                var product = ElementDefinitionInfo.FromSourceNode(sdCanonical, sdTypeName, elementDefinitionNodes[current..]);
                yield return product;
                current++;
            }
            while (current < elementDefinitionNodes.Count && pathLength() == initialLength);

            int pathLength()
            {
                var path = elementDefinitionNodes[current].ChildString("path");
                if (path is null) return 0;

                int length = path.Length;
                int count = 0;
                for (int n = length - 1; n >= 0; n--)
                {
                    if (path[n] == '.') count++;
                }

                return count;
            }
        }

    }

}
