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

            var pathAndElements = elementNodes.Select(en => makeNode(en)).ToArray();
            addElements(newSd, newSd, pathAndElements);

            return newSd;

            static (string path, ISourceNode elementNode) makeNode(ISourceNode n)
            {
                var path = n.ChildString("path") ?? throw new InvalidOperationException("Encountered an ElementNode without a path.");
                return (path, n);
            }


        }

        internal static string TypeNameForBackbone(StructureDefinitionInfo rootSd, (string path, ISourceNode node) backboneNode)
        {
            return rootSd.TypeName + "#" + (getExplicitTypeName() ?? pascalBackboneName());

            string? getExplicitTypeName() => backboneNode.node
                .GetExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-explicit-type-name")?
                .ChildString("valueString");

            string pascalBackboneName()
            {
                var backbonePathEnd = backboneNode.path.Split(".")[^1];
                return char.ToUpperInvariant(backbonePathEnd[0]) + backbonePathEnd[1..];
            }
        }

        internal static StructureDefinitionInfo FromBackbone(StructureDefinitionInfo parentSd, StructureDefinitionInfo rootSd, string elementName, string backboneType,
            ArraySegment<(string path, ISourceNode node)> backboneNodes)
        {
            if (!backboneNodes.Any()) throw new ArgumentException("Cannot read a backbone from an empty list of ISourceNodes.");
            ISourceNode backboneRoot = backboneNodes[0].node;

            var backboneTypeName = TypeNameForBackbone(rootSd, backboneNodes[0]);
            var backboneCanonical = rootSd.Canonical + "#" + backboneRoot.ChildString("path");

            var backboneSd = new StructureDefinitionInfo(backboneCanonical, backboneTypeName, IsAbstract: false, IsResource: false, backboneType);
            addElements(backboneSd, rootSd, backboneNodes[1..]);
            return backboneSd;
        }

        private static void addElements(StructureDefinitionInfo parentSd, StructureDefinitionInfo rootSd, ArraySegment<(string path, ISourceNode node)> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) return;

            // Track the length of the path of the current element, so we can
            // scan for just the sibling elements of the current node
            int current = 0;
            var initialLength = pathLength(elementDefinitionNodes[current].path);

            do
            {
                var childPathPrefix = elementDefinitionNodes[current].path + ".";
                var end = current;
                while (end < elementDefinitionNodes.Count && (end == current || elementDefinitionNodes[end].path.StartsWith(childPathPrefix)))
                    end++;

                _ = ElementDefinitionInfo.AddFromSourceNode(parentSd, rootSd, elementDefinitionNodes[current..end]);

                current = end;
            }
            while (current < elementDefinitionNodes.Count && pathLength(elementDefinitionNodes[current].path) >= initialLength);

            static int pathLength(string path)
            {
                if (path is null) return 0;

                int length = path.Length;
                int count = 0;
                for (int n = length - 1; n >= 0; n--)
                    if (path[n] == '.') count++;

                return count;
            }
        }

        public ElementDefinitionInfo AddElementDefinitionInfo(string elementName, string fullPath, ElementDefinitionTypeRef[]? typeRefs,
            StructureDefinitionInfo? backbone, string? contentReference, bool isChoiceElement, bool isCollection, int? min, string? max)
        {
            var newElement = new ElementDefinitionInfo(elementName, fullPath, typeRefs, backbone, contentReference, isChoiceElement,
                isCollection, min, max, this);
            _elements.Add(newElement);

            return newElement;
        }
    }

}
