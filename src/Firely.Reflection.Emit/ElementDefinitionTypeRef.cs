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
        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private const string SDXMLTYPEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type";

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
                return typeRefs.Select(tr => fromTypeRef(tr)).ToArray();
            }
        }

        private static ElementDefinitionTypeRef fromTypeRef(ISourceNode typeRef)
        {
            var type = typeRef.ChildString("code"); // in R4+, this will always contain the type name (including System primitive canonicals).

            if (type is null)
            {
                // in R3, we could now have hit a primitive, which we need to derive from the structuredefinition-xml-type extension, e.g.
                // <extension url="http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type">
                //      <valueString value = "xsd:int" /> 
                // </extension>
                var xsdTypeName = typeRef.GetStringExtension(SDXMLTYPEEXTENSION);
                if (xsdTypeName is null) throw new InvalidOperationException("Encountered a typeref with neither a code nor primitive type compiler magic.");

                type = deriveSystemTypeFromXsdType(xsdTypeName);
            }

            var targetProfiles = typeRef.Children("targetProfile").Select(c => c.Text).ToArray();

            return new ElementDefinitionTypeRef(type, targetProfiles);

            static string deriveSystemTypeFromXsdType(string xsdTypeName)
            {
                // This R3-specific mapping is derived from the possible xsd types from the primitive datatype table
                // at http://www.hl7.org/fhir/stu3/datatypes.html, and the mapping of these types to
                // FhirPath from http://hl7.org/fhir/fhirpath.html#types
                return SYSTEMTYPEURI + (xsdTypeName switch
                {
                    "xsd:boolean" => "Boolean",
                    "xsd:int" => "Integer",
                    "xsd:string" => "String",
                    "xsd:decimal" => "Decimal",
                    "xsd:anyURI" => "String",
                    "xsd:base64Binary" => "String",
                    "xsd:dateTime" => "DateTime",
                    "xsd:gYear OR xsd:gYearMonth OR xsd:date" => "DateTime",
                    "xsd:gYear OR xsd: gYearMonth OR xsd: date OR xsd: dateTime" => "DateTime",
                    "xsd:time" => "Time",
                    "xsd:token" => "String",
                    "xsd:nonNegativeInteger" => "Integer",
                    "xsd:positiveInteger" => "Integer",
                    "xhtml:div" => "String", // used in R3 xhtml
                    _ => throw new NotSupportedException($"The xsd type {xsdTypeName} is not supported as a primitive type in R3.")
                }); ;
            }
        }
    }

}
