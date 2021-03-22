/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using System;
using System.Linq;

namespace Firely.Reflection.Emit
{
    /// <summary>
    /// Holds the information for an element from the differential of a StructureDefinition that is relevant 
    /// for dynamically generating .NET System.Type.
    /// </summary>
    /// <param name="Name">Name of the type to be generated.</param>
    /// <param name="TypeRef">The list of <see cref="ElementDefinitionTypeRef"/> that represent the type(s) of the element.</param>
    /// <param name="Backbone">If this is a backbone element: a nested <see cref="StructureDefinitionInfo"/>, otherwise <c>null</c>.</param>
    /// <param name="ContentReference">If this is a backbone element: a nested <see cref="StructureDefinitionInfo"/>, otherwise <c>null</c>.</param>
    internal record ElementDefinitionInfo(string Name, ElementDefinitionTypeRef[]? TypeRef, StructureDefinitionInfo? Backbone,
        bool IsChoice, bool IsCollection, int? Min, string? Max, int Order, XmlRepresentation Representation, bool IsPrimitiveValue, bool InSummary,
        string? DefaultTypeName, string? NonDefaultNamespace)
    {
        // public StructureDefinitionInfo? Parent { get; internal set; }

        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private const string ELEMENTDEFDEFAULTTYPEEXTENSION = @"http://hl7.org/fhir/StructureDefinition/elementdefinition-defaulttype";
        private const string ELEMENTDEFNAMESPACEEXTENSION = @"http://hl7.org/fhir/StructureDefinition/elementdefinition-namespace";
        private static readonly string[] BACKBONEELEMENTNAMES = new[] { "BackboneElement", "Element" };

        /// <summary>
        /// Generates the information for an element from a single element + its child elements (in case of a backbone element).
        /// </summary>        
        /// <param name="elementDefinitionNodes">The element, and its children as a flat list.</param>
        internal static ElementDefinitionInfo FromElementDefinition(StructureDefinitionInfo rootSd, int order, ArraySegment<(string path, ISourceNode node)> elementDefinitionNodes)
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
            bool inSummary = "true" == elementDefinitionNode.ChildString("isSummary");

            string? id = elementDefinitionNode.ChildString("id");
            string? contentReference = elementDefinitionNode.ChildString("contentReference");

            var typeCodes = elementDefinitionNode.Children("type").Children("code").Select(tc => tc.Text).ToArray();

            bool isBackboneElement = BACKBONEELEMENTNAMES.Contains(typeCodes.FirstOrDefault());

            var (element, @base) = getCardinality("max", elementDefinitionNode);
            var min = int.TryParse(getCardinality("min", elementDefinitionNode).element, out int m) ? m : default(int?);

            var defaultTypeName = elementDefinitionNode.GetStringExtension(ELEMENTDEFDEFAULTTYPEEXTENSION);
            var representation = translateXmlRepresentation(elementDefinitionNode.ChildString("representation"));
            var nonDefaultNamespace = elementDefinitionNode.GetStringExtension(ELEMENTDEFNAMESPACEEXTENSION);

            // CK: The .max of this element may be constrained to 1, whereas the .max of the base is actually *.
            // Then this element is still a collection.
            var isCollection = ((@base ?? element) is string max) && (max != "0" && max != "1");

            ElementDefinitionTypeRef[]? typeRefs = null;
            StructureDefinitionInfo? backbone = null;

            if (isBackboneElement)
            {
                var code = typeCodes.First();
                backbone = StructureDefinitionInfo.FromBackbone(rootSd, elementName, code, elementDefinitionNodes);
            }
            else if (contentReference is not null)
            {
                var referencedNodes = elementDefinitionNodes.Array.Where(all => all.path == contentReference[1..]);
                if (!referencedNodes.Any()) throw new InvalidOperationException($"Cannot find the node referenced by the contentreference path '{contentReference}'.");

                var backboneTypeName = StructureDefinitionInfo.TypeNameForBackbone(rootSd, referencedNodes.First());
                typeRefs = new[] { new ElementDefinitionTypeRef(backboneTypeName, Array.Empty<string>()) };
            }
            else
            {
                typeRefs = ElementDefinitionTypeRef.FromElementDefinition(elementDefinitionNode);
            }

            var isPrimitiveValue = elementName == "value" && typeRefs?.First().IsSystemType == true;

            return new ElementDefinitionInfo(elementName, typeRefs, backbone,
                isChoiceElement, isCollection, min, element, order, representation, isPrimitiveValue, inSummary, defaultTypeName, nonDefaultNamespace);
        }

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

        private static XmlRepresentation translateXmlRepresentation(string? input) =>
          input switch
          {
              "xmlAttr" => XmlRepresentation.XmlAttr,
              "xmlText" => XmlRepresentation.XmlText,
              "typeAttr" => XmlRepresentation.TypeAttr,
              "cdaText" => XmlRepresentation.CdaText,
              "xhtml" => XmlRepresentation.XHtml,
              _ => XmlRepresentation.XmlElement
          };
    }

}
