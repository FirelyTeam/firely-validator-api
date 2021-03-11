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

            var elements = ElementDefinitionInfo.FromSourceNodes(canonical, elementNodes).ToArray();

            return new StructureDefinitionInfo(canonical, typeName, isAbstract, isResource, baseDefinition, elements);
        }

        public static (StructureDefinitionInfo product, ArraySegment<ISourceNode> rest) FromBackbone(string parentCanonical, string backboneType, ArraySegment<ISourceNode> backboneNode)
        {
            /*
             * 
             *  backboneName = elementDefinition.getextension("http://hl7.org/fhir/StructureDefinition/structuredefinition-explicit-type-name").ValueString;
             *  missingBackboneName = ToPascal(_name (=last part of path));
             *  typename = parentType + "#" + (backboneName ?? missingBackboneName)
             *  canonical = parentCanonical + "#" + backbonePath
             */
            var backboneCanonical = (string)null ?? throw new NotImplementedException();
            var backboneTypeName = (string)null ?? throw new NotImplementedException();
            var elements = ElementDefinitionInfo.FromSourceNodes(backboneCanonical, backboneNode[1..^0]).ToArray();
            var product = new StructureDefinitionInfo(backboneCanonical, backboneTypeName, IsAbstract: false, IsResource: false, backboneType, elements);
            return (product, null);
        }
    }

}
