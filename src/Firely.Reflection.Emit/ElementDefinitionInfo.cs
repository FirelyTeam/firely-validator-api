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
    internal record ElementDefinitionInfo(string Name, string Path, ElementDefinitionTypeRef[] TypeRef,
        string? ContentReference, bool IsBackboneElement, bool IsPrimitive)
    {
        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private const string JSONTYPEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-json-type";
        private static readonly string[] BACKBONEELEMENTNAMES = new[] { "BackboneElement", "Element" };

        private readonly Lazy<string[]> _pathParts = new(() => Path.Split("."));

        public string[] PathParts => _pathParts.Value;

        public static ElementDefinitionInfo FromSourceNode(ISourceNode elementDefinitionNode)
        {
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
            bool isSlice = elementDefinitionNode.ChildString("sliceName") is string;
            bool isPrimitive = getIsPrimitiveTypeConstraint(path[^1], typeCodes);

            var typeRefs = ElementDefinitionTypeRef.FromSourceNode(elementDefinitionNode);

            return new ElementDefinitionInfo(elementName, fullPath, typeRefs, contentReference, isBackboneElement, isPrimitive);
        }

        public bool IsContentReference => ContentReference is not null;

        private static bool getIsPrimitiveTypeConstraint(string lastPathPart, ISourceNode[] typeCodes)
        {
            // primitive value members are never choice types, so there must be a single code too
            if (lastPathPart == "value" && typeCodes.Length == 1)
            {
                var typeCode = typeCodes.Single();

                if (typeCode.Text is string)
                {
                    //Since R4: explicit system types
                    return typeCode.Text.StartsWith(SYSTEMTYPEURI);
                }
                else
                {
                    var extension = typeCode.GetStringExtension(JSONTYPEXTENSION); //Until R4: extensions used to specify the native (json and xml) types
                    return extension is string;
                }
            }

            return false;
        }

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
