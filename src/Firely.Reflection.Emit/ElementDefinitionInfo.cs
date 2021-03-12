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
    internal record ElementDefinitionInfo(StructureDefinitionInfo Parent, string Name, string Path, ElementDefinitionTypeRef[]? TypeRef, StructureDefinitionInfo? Backbone,
        string? ContentReference, bool IsChoice, string? Max)
    {
        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private static readonly string[] BACKBONEELEMENTNAMES = new[] { "BackboneElement", "Element" };

        public static ElementDefinitionInfo AddFromSourceNode(StructureDefinitionInfo parentSd, ArraySegment<ISourceNode> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) throw new InvalidOperationException("Cannot construct an Element from an empty set of ElementDefinitions.");

            var elementDefinitionNode = elementDefinitionNodes.First();
            var fullPath = elementDefinitionNode.ChildString("path") ?? throw new InvalidOperationException("Encountered an ElementNode without a path.");
            string[] path = fullPath.Split('.');
            string elementName = path.Last();

            string[]? basePath = elementDefinitionNode.Child("base")?.ChildString("path")?.Split('.');
            string? basePathElement = basePath?.Last();

            bool isChoiceElement = false;

            if (basePathElement?.EndsWith("[x]") == true || elementName.EndsWith("[x]"))
            {
                elementName = elementName[0..^3];
                isChoiceElement = true;
            }

            string? max = getMax(elementDefinitionNode);
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
                backbone = StructureDefinitionInfo.FromBackbone(parentSd, elementName, code, elementDefinitionNodes);
            }

            return parentSd.AddElementDefinitionInfo(elementName, fullPath, typeRefs, backbone, contentReference,
                isChoiceElement, max);
        }

        public bool IsCollection => Max is not null && Max != "0" && Max != "1";

        public bool IsContentReference => ContentReference is not null;

        private static string? getMax(ISourceNode elementDefinitionNode)
        {
            // CK: The .max of this element may be constrained to 1, whereas the .max of the base is actually *.
            // Then this element is still a collection.
            var baseMax = elementDefinitionNode.Child("base")?.ChildString("max");
            var eltMax = elementDefinitionNode.ChildString("max");
            return baseMax ?? eltMax;
        }

        private static bool getIsRequired(ISourceNode elementDefinitionNode)
        {
            var min = elementDefinitionNode.ChildString("min") ?? elementDefinitionNode.Child("base")?.ChildString("min");
            return !(min == "0");
        }

        private static bool getInSummary(ISourceNode elementDefinitionNode) => "true" == elementDefinitionNode.ChildString("isSummary");
    }

}
