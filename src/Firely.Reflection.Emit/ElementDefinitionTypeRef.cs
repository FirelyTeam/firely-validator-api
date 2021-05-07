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
    /// <summary>
    /// Holds the information for type of an element that is relevant for dynamically generating .NET System.Type.
    /// </summary>
    /// <param name="Type">The name of the type of the element.  This may be a uri for logical models or when
    /// a (CQL) type from the System namespace is used.</param>
    /// <param name="TargetTypeUris">The list of target types allowed (as a canonical) when this type is a reference
    /// (for example a FHIR "Reference" or "canonical" datatype).</param>
    internal record ElementDefinitionTypeRef(string Type, string[]? TargetTypeUris)
    {
        public bool IsSystemType => Type.StartsWith(SYSTEMTYPEURI);

        public bool IsResourceType => Type == "Resource" || Type == "DomainResource";

        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        private const string SDXMLTYPEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type";

        public static ElementDefinitionTypeRef[] FromElementDefinition(ISourceNode elementDefinitionNode)
        {
            var path = elementDefinitionNode.ChildText("path");

            if (path == "Resource.id")
            {
                // [MV 20191217] it should be url?.Value, but there is something wrong in the 
                // specification (https://jira.hl7.org/browse/FHIR-25262), so I manually change it to "id".
                return new[] { new ElementDefinitionTypeRef("id", null) };
            }
            else if (path == "Element.id")
            {
                // Element.id (and thus all ids from the subclasses, are incorrectly set to FHIR.String in R3)
                return new[] { new ElementDefinitionTypeRef(makeSystemType("String"), null) };
            }
            else
            {
                var typeRefs = elementDefinitionNode.Children("type") ?? throw new InvalidOperationException("Encountered an ElementDefinition without typeRefs.");
                return typeRefs.Select(tr => fromTypeRef(tr)).ToArray();
            }
        }

        private static string makeSystemType(string name) => SYSTEMTYPEURI + name;

        private static ElementDefinitionTypeRef fromTypeRef(ISourceNode typeRef)
        {
            var type = typeRef.ChildText("code"); // in R4+, this will always contain the type name (including System primitive canonicals).

            if (type is null)
            {
                // in R3, we could now have hit a primitive, which we need to derive from the structuredefinition-xml-type extension, e.g.
                // <extension url="http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type">
                //      <valueString value = "xsd:int" /> 
                // </extension>
                var code = typeRef.Child("code");
                var xsdTypeName = code?.GetStringExtension(SDXMLTYPEEXTENSION);
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
                return makeSystemType(xsdTypeName switch
                {
                    "xsd:boolean" => "Boolean",
                    "xsd:int" => "Integer",
                    "xsd:string" => "String",
                    "xsd:decimal" => "Decimal",
                    "xsd:anyURI" => "String",
                    "xsd:base64Binary" => "String",
                    "xsd:dateTime" => "DateTime",
                    "xsd:gYear OR xsd:gYearMonth OR xsd:date" => "DateTime",
                    "xsd:gYear OR xsd:gYearMonth OR xsd:date OR xsd:dateTime" => "DateTime",
                    "xsd:time" => "Time",
                    "xsd:token" => "String",
                    "xsd:nonNegativeInteger" => "Integer",
                    "xsd:positiveInteger" => "Integer",
                    "xhtml:div" => "String", // used in R3 xhtml
                    _ => throw new NotSupportedException($"The xsd type {xsdTypeName} is not supported as a primitive type in R3.")
                });
            }
        }
    }

}
