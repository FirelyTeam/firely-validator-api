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
    /// <summary>
    /// Holds the information from a StructureDefinition that is relevant for dynamically generating .NET System.Type.
    /// </summary>
    /// <param name="Canonical">The canonical url for the StructureDefinition, taken from the 'url' element.</param>
    /// <param name="TypeName">The name of the type being defined, taken from the 'name' element.</param>
    /// <param name="IsAbstract">Whether this type is marked as abstract in the StructureDefinition.</param>
    /// <param name="Kind">The kind of datatype (resource, backbone, complex, etc.) this StructureDefinition describes.</param>
    /// <param name="BaseCanonical">The base type of the type being defined, which is a canonical from the 'baseDefinition' element.</param>
    /// <remarks>The TypeName for backbone elements found within a StructureDefinition is the name of the parent type + "#" +
    /// either an explicitly defined backbone name or, if unavailable, the name of the element that contains the backbone.</remarks>
    internal record StructureDefinitionInfo(
        string Canonical,
        string TypeName,
        bool IsAbstract,
        StructureDefinitionKind Kind,
        string? BaseCanonical)
    {
        private const string SDEXPLICITTYPENAMEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-explicit-type-name";
        private readonly List<ElementDefinitionInfo> _elements = new();
        public IReadOnlyList<ElementDefinitionInfo> Elements => _elements;

        public static StructureDefinitionInfo FromStructureDefinition(ISourceNode structureDefNode)
        {
            var canonical = structureDefNode.ChildText("url") ?? throw new InvalidOperationException("Missing 'url' in the StructureDefinition.");
            var typeName = structureDefNode.ChildText("name") ?? throw new InvalidOperationException("Missing 'name' in the StructureDefinition.");
            var baseDefinition = structureDefNode.ChildText("baseDefinition");
            var isAbstract = "true" == structureDefNode.ChildText("abstract");

            var kind = structureDefNode.ChildText("kind") switch
            {
                "primitive-type" => StructureDefinitionKind.Primitive,
                "complex-type" => StructureDefinitionKind.Complex,
                "resource" => StructureDefinitionKind.Resource,
                //"logical" => StructureDefinitionKind.Logical,
                var other => throw new NotSupportedException($"Don't know how to handle StructureDefinitions of kind '{other}'.")
            };

            var elementNodes = structureDefNode.Child("differential")?.Children("element")?.Skip(1).ToArray()
                        ?? Array.Empty<ISourceNode>();

            var newSd = new StructureDefinitionInfo(canonical, typeName, isAbstract, kind, baseDefinition);

            var pathAndElements = elementNodes.Select(en => makeNode(en)).ToArray();
            addElements(newSd, newSd, pathAndElements);

            return newSd;

            static (string path, ISourceNode elementNode) makeNode(ISourceNode n)
            {
                var path = n.ChildText("path") ?? throw new InvalidOperationException("Encountered an ElementNode without a path.");
                return (path, n);
            }
        }

        internal static StructureDefinitionInfo FromBackbone(StructureDefinitionInfo rootSd, string elementName, string backboneType,
            ArraySegment<(string path, ISourceNode node)> backboneNodes)
        {
            if (!backboneNodes.Any()) throw new ArgumentException("Cannot read a backbone from an empty list of ISourceNodes.");
            ISourceNode backboneRoot = backboneNodes[0].node;

            var backboneTypeName = TypeNameForBackbone(rootSd, backboneNodes[0]);
            var backboneCanonical = rootSd.Canonical + "_" + backboneRoot.ChildText("path");

            var backboneSd = new StructureDefinitionInfo(
                backboneCanonical,
                backboneTypeName,
                IsAbstract: false,
                StructureDefinitionKind.Backbone,
                "http://hl7.org/fhir/StructureDefinition/" + backboneType);
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
                var order = parentSd._elements.Count;

                var newElement = ElementDefinitionInfo.FromElementDefinition(rootSd, order, elementDefinitionNodes[current..end]);
                // newElement.Parent = parentSd;
                parentSd._elements.Add(newElement);

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

        internal static string TypeNameForBackbone(StructureDefinitionInfo rootSd, (string path, ISourceNode node) backboneNode)
        {
            return rootSd.TypeName + "_" + (getExplicitTypeName() ?? pascalBackboneName());

            string? getExplicitTypeName() => backboneNode.node
                .GetExtension(SDEXPLICITTYPENAMEEXTENSION)?
                .ChildText("valueString");

            string pascalBackboneName()
            {
                var backbonePathEnd = backboneNode.path.Split(".")[^1];
                return char.ToUpperInvariant(backbonePathEnd[0]) + backbonePathEnd[1..];
            }
        }
    }

}
