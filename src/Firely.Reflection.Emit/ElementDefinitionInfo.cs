/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System;
using System.Linq;

namespace Firely.Reflection.Emit
{
    internal record ElementDefinitionInfo(string Name, string Path, ElementDefinitionTypeRef[]? TypeRef, StructureDefinitionInfo? Backbone,
        string? ContentReference, bool IsChoice, bool IsCollection)
    {
        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private static readonly string[] BACKBONEELEMENTNAMES = new[] { "BackboneElement", "Element" };

        private readonly Lazy<string[]> _pathParts = new(() => Path.Split("."));

        public string[] PathParts => _pathParts.Value;

        public static ElementDefinitionInfo FromSourceNode(string sdCanonical, string sdTypeName, ArraySegment<ISourceNode> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) throw new InvalidOperationException("Cannot construct an Element from an empty set of ElementDefinitions.");

            var elementDefinitionNode = elementDefinitionNodes.First();
            var fullPath = elementDefinitionNode.ChildString("path") ?? throw new InvalidOperationException("Encountered an ElementNode without a path.");
            string[] path = fullPath.Split('.');
            string elementName = path.Last();
            bool isChoiceElement = false;

            if (elementName.EndsWith("[x]"))
            {
                elementName = elementName[0..^3];
                isChoiceElement = true;
            }

            bool isCollection = getIsCollection(elementDefinitionNode);
            bool isRequired = getIsRequired(elementDefinitionNode);
            bool inSummary = getInSummary(elementDefinitionNode);

            string? id = elementDefinitionNode.ChildString("id");
            string? contentReference = elementDefinitionNode.ChildString("contentReference");

            var typeCodes = elementDefinitionNode.Children("type").Children("code").ToArray();

            bool isBackboneElement = BACKBONEELEMENTNAMES.Contains(typeCodes.Select(tc => tc.Text).FirstOrDefault());

            ElementDefinitionTypeRef[]? typeRefs = null;
            StructureDefinitionInfo? backbone = null;

            if (!isBackboneElement)
            {
                typeRefs = ElementDefinitionTypeRef.FromSourceNode(elementDefinitionNode);
            }
            else
            {
                var code = typeCodes.First().Text;
                backbone = StructureDefinitionInfo.FromBackbone(elementName, sdCanonical, sdTypeName, code, elementDefinitionNodes);
            }

            return new ElementDefinitionInfo(elementName, fullPath, typeRefs, backbone, contentReference,
                isChoiceElement, isCollection);
        }

        public bool IsContentReference => ContentReference is not null;

        private static bool getIsCollection(ISourceNode elementDefinitionNode)
        {
            var baseMax = elementDefinitionNode.Child("base")?.ChildString("max"); //CK: The .max of this element may be constrained to 1, whereas the .max of the base is actually *. Then this element is still a collection.
            var eltMax = elementDefinitionNode.ChildString("max");
            return baseMax is string ? baseMax != "0" && baseMax != "1" && eltMax != "0" : eltMax != "0" && eltMax != "1";
        }

        private static bool getIsRequired(ISourceNode elementDefinitionNode)
        {
            var min = elementDefinitionNode.ChildString("min") ?? elementDefinitionNode.Child("base")?.ChildString("min");
            return !(min == "0");
        }

        private static bool getInSummary(ISourceNode elementDefinitionNode) => "true" == elementDefinitionNode.ChildString("isSummary");
    }

}
