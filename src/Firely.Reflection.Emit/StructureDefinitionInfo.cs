/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System;

namespace Firely.Reflection.Emit
{
    internal record StructureDefinitionInfo(string Canonical, string TypeName, bool IsAbstract, bool IsResource, string? BaseDefinition)
    {
        public static StructureDefinitionInfo FromSourceNode(ISourceNode structureDefNode)
        {
            var canonical = structureDefNode.ChildString("url") ?? throw new InvalidOperationException("Missing 'url' in the StructureDefinition.");
            var typeName = structureDefNode.ChildString("name") ?? throw new InvalidOperationException("Missing 'name' in the StructureDefinition.");
            var baseDefinition = structureDefNode.ChildString("baseDefinition");
            var isAbstract = "true" == structureDefNode.ChildString("abstract");
            var isResource = "resource" == structureDefNode.ChildString("kind");

            return new StructureDefinitionInfo(canonical, typeName, isAbstract, isResource, baseDefinition);
        }
    }

}
