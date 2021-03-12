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
        string? ContentReference, bool IsChoice, bool IsCollection, int? Min, string? Max, StructureDefinitionInfo Parent)
    {
        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private static readonly string[] BACKBONEELEMENTNAMES = new[] { "BackboneElement", "Element" };

        internal static ElementDefinitionInfo AddFromSourceNode(StructureDefinitionInfo parentSd, StructureDefinitionInfo rootSd, ArraySegment<(string path, ISourceNode node)> elementDefinitionNodes)
        {
            if (!elementDefinitionNodes.Any()) throw new InvalidOperationException("Cannot construct an Element from an empty set of ElementDefinitions.");

            var (fullPath, elementDefinitionNode) = elementDefinitionNodes.First();

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

            bool isRequired = getIsRequired(elementDefinitionNode);
            bool inSummary = getInSummary(elementDefinitionNode);

            string? id = elementDefinitionNode.ChildString("id");
            string? contentReference = elementDefinitionNode.ChildString("contentReference");

            var typeCodes = elementDefinitionNode.Children("type").Children("code").Select(tc => tc.Text).ToArray();

            bool isBackboneElement = BACKBONEELEMENTNAMES.Contains(typeCodes.FirstOrDefault());

            var maxCard = getCardinality("max", elementDefinitionNode);
            var min = int.TryParse(getCardinality("min", elementDefinitionNode).element, out int m) ? m : default(int?);

            // CK: The .max of this element may be constrained to 1, whereas the .max of the base is actually *.
            // Then this element is still a collection.
            var isCollection = ((maxCard.@base ?? maxCard.element) is string max) && (max != "0" && max != "1");

            ElementDefinitionTypeRef[]? typeRefs = null;
            StructureDefinitionInfo? backbone = null;

            if (isBackboneElement)
            {
                var code = typeCodes.First();
                backbone = StructureDefinitionInfo.FromBackbone(parentSd, rootSd, elementName, code, elementDefinitionNodes);
            }
            else if (contentReference is not null)
            {
                //// a content reference looks like #TypeName.path1.path2....
                //// Skip the # and the first part of the parth so we can navigate over the childnames.
                //var contentReferenceParts = contentReference[1..].Split('.')[1..];
                //if (!contentReferenceParts.Any()) throw new InvalidOperationException($"Encountered an invalid contentReference {contentReference}.");

                //StructureDefinitionInfo? backboneSd = parentSd;
                //ElementDefinitionInfo? current = null;
                //foreach (var part in contentReferenceParts)
                //{
                //    current = backboneSd?.Elements.FirstOrDefault(e => e.Name == part);
                //    if (current is null) throw new InvalidOperationException($"Cannot find child '{part}' in the contentreference path '{contentReference}'.");
                //    backboneSd = current.Backbone;
                //}

                //if (backboneSd is null) throw new InvalidOperationException($"Content reference '{contentReference}' does not refer to a backbone element.");

                var referencedNodes = elementDefinitionNodes.Array.Where(all => all.path == contentReference[1..]);
                if (!referencedNodes.Any()) throw new InvalidOperationException($"Cannot find the node referenced by the contentreference path '{contentReference}'.");

                var backboneTypeName = StructureDefinitionInfo.TypeNameForBackbone(rootSd, referencedNodes.First());
                typeRefs = new[] { new ElementDefinitionTypeRef(backboneTypeName, Array.Empty<string>()) };
            }
            else
            {
                typeRefs = ElementDefinitionTypeRef.FromSourceNode(elementDefinitionNode);
            }


            return parentSd.AddElementDefinitionInfo(elementName, fullPath, typeRefs, backbone, contentReference,
                isChoiceElement, isCollection, min, maxCard.element);
        }

        public bool IsContentReference => ContentReference is not null;

        private static (string? element, string? @base) getCardinality(string child, ISourceNode elementDefinitionNode)
        {
            var @base = elementDefinitionNode.Child("base")?.ChildString(child);
            var elt = elementDefinitionNode.ChildString(child);
            return (elt, @base);
        }

        private static bool getIsRequired(ISourceNode elementDefinitionNode)
        {
            var min = elementDefinitionNode.ChildString("min") ?? elementDefinitionNode.Child("base")?.ChildString("min");
            return !(min == "0");
        }

        private static bool getInSummary(ISourceNode elementDefinitionNode) => "true" == elementDefinitionNode.ChildString("isSummary");
    }

}
