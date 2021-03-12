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
    internal record StructureDefinitionInfo(string Canonical, string TypeName, bool IsAbstract, bool IsResource, string? BaseCanonical)
    {
        private readonly List<ElementDefinitionInfo> _elements = new();
        public IReadOnlyList<ElementDefinitionInfo> Elements => _elements;

        public static StructureDefinitionInfo FromStructureDefinition(ISourceNode structureDefNode)
        {
            var canonical = structureDefNode.ChildString("url") ?? throw new InvalidOperationException("Missing 'url' in the StructureDefinition.");
            var typeName = structureDefNode.ChildString("name") ?? throw new InvalidOperationException("Missing 'name' in the StructureDefinition.");
            var baseDefinition = structureDefNode.ChildString("baseDefinition");
            var isAbstract = "true" == structureDefNode.ChildString("abstract");
            var isResource = "resource" == structureDefNode.ChildString("kind");

            var elementNodes = structureDefNode.Child("differential")?.Children("element")?.Skip(1).ToArray()
                        ?? Array.Empty<ISourceNode>();

            var newSd = new StructureDefinitionInfo(canonical, typeName, isAbstract, isResource, baseDefinition);

            addElements(newSd, elementNodes);

            return newSd;
        }

        public static StructureDefinitionInfo FromBackbone(StructureDefinitionInfo parentSd, string elementName, string backboneType, ArraySegment<ISourceNode> backboneNode)
        {
            if (!backboneNode.Any()) throw new ArgumentException("Cannot read a backbone from an empty list of ISourceNodes.");
            ISourceNode backboneRoot = backboneNode[0]!;

            var backboneTypeName = parentSd.TypeName + "#" + (getExplicitTypeName() ?? pascalBackboneName());
            var backboneCanonical = parentSd.Canonical + "#" + backboneRoot.ChildString("path");

            addElements(parentSd, backboneNode[1..]);
            return new StructureDefinitionInfo(backboneCanonical, backboneTypeName, IsAbstract: false, IsResource: false, backboneType);

            string? getExplicitTypeName() => backboneRoot.GetExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-explicit-type-name")?
                .ChildString("valueString");
            string pascalBackboneName() => char.ToUpperInvariant(elementName[0]) + elementName[1..];
        }

        private static void addElements(StructureDefinitionInfo parentSd, ArraySegment<ISourceNode> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) return;

            var current = 0;

            // Track the length of the path of the current element, so we can
            // scan for just the sibling elements of the current node
            var initialLength = pathLength();

            do
            {
                _ = ElementDefinitionInfo.AddFromSourceNode(parentSd, elementDefinitionNodes[current..]);
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

        public ElementDefinitionInfo AddElementDefinitionInfo(string elementName, string fullPath, ElementDefinitionTypeRef[]? typeRefs, StructureDefinitionInfo? backbone, string? contentReference, bool isChoiceElement, string? max)
        {
            var newElement = new ElementDefinitionInfo(this, elementName, fullPath, typeRefs, backbone, contentReference, isChoiceElement, max);
            _elements.Add(newElement);

            return newElement;
        }
    }

}
