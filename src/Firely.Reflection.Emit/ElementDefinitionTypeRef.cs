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
    internal record ElementDefinitionTypeRef(string Type, string[]? TargetProfiles)
    {
        private const string SYSTEMTYPE = "http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type";

        private static ElementDefinitionTypeRef getNormalTypeRef(ISourceNode typeRef)
        {
            var code = typeRef.ChildString("code") ?? throw new InvalidOperationException("Encountered a non-primitive typeref without a code.");
            var targetProfiles = typeRef.Children("targetProfile").Select(c => c.Text).ToArray();

            return new ElementDefinitionTypeRef(code, targetProfiles);
        }

        public static ElementDefinitionTypeRef[] FromSourceNode(ISourceNode elementDefinitionNode)
        {
            // Don't need the ed.base.path here, since we're generating from a differential,
            // and id is only generated on the actual Resource type.
            var path = elementDefinitionNode.ChildString("path");

            if (path == "Resource.id")
            {
                // [MV 20191217] it should be url?.Value, but there is something wrong in the 
                // specification (https://jira.hl7.org/browse/FHIR-25262), so I manually change it to "id".
                return new[] { new ElementDefinitionTypeRef("id", null) };
            }
            else if (path == "xhtml.id")
            {
                // [EK 20200423] xhtml.id is missing the structuredefinition-fhir-type extension
                return new[] { new ElementDefinitionTypeRef("string", null) };
            }
            else
            {
                var typeRefs = elementDefinitionNode.Children("type") ?? throw new InvalidOperationException("Encountered an ElementDefinition without typeRefs.");
                var fhirTypeExtension = typeRefs.FirstOrDefault()?.GetExtension(SYSTEMTYPE);

                if (fhirTypeExtension is not null)
                {
                    //R3, R4: extension value is of type url, hence 'valueUrl'.
                    //R5: extension value is of type uri, hence 'valueUri'.
                    var systemType = fhirTypeExtension.ChildString("valueUrl") ?? fhirTypeExtension.ChildString("valueUri") ??
                        throw new InvalidOperationException($"Encountered a {SYSTEMTYPE} extension without a value[x]");

                    return new[] { new ElementDefinitionTypeRef(systemType, null) };
                }
                else
                    return typeRefs.Select(tr => getNormalTypeRef(tr)).ToArray();
            }
        }
    }

}
