/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{

    internal static class CommonTypeRefComponentExtensions
    {
        internal const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        internal const string SDXMLTYPEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type";

        // TODO: This would probably be useful for the SDK too
        internal static string GetCodeFromTypeRef(this CommonTypeRefComponent typeRef)
        {
            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes),
            // and there are some R4 profiles in the wild that still use this old schema too.
            if (string.IsNullOrEmpty(typeRef.Code))
            {
                var r3TypeIndicator = typeRef.CodeElement?.GetStringExtension(SDXMLTYPEEXTENSION) ?? throw new IncorrectElementDefinitionException($"Encountered a typeref without a code nor xml-type extension..");
                return deriveSystemTypeFromXsdType(r3TypeIndicator);
            }
            else
                return typeRef.Code;

            static string deriveSystemTypeFromXsdType(string xsdTypeName)
            {
                // This R3-specific mapping is derived from the possible xsd types from the primitive datatype table
                // at http://www.hl7.org/fhir/stu3/datatypes.html, and the mapping of these types to
                // FhirPath from http://hl7.org/fhir/fhirpath.html#types
                var systemType = xsdTypeName switch
                {
                    "xsd:boolean" => "Boolean",
                    "xsd:int" => "Integer",
                    "xsd:string" => "String",
                    "xsd:decimal" => "Decimal",
                    "xsd:anyURI" => "String",
                    "xsd:anyUri" => "String",
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
                };

                return SYSTEMTYPEURI + systemType;
            }
        }

        /// <summary>
        ///    Returns the profiles on the given Hl7.Fhir.Model.ElementDefinition.TypeRefComponent
        ///     if specified, or otherwise the core profile url for the specified type code.
        /// </summary>
        // TODO: This function can be replaced by the equivalent SDK function when the current bug is resolved.
        internal static IEnumerable<string>? GetTypeProfilesCorrect(this CommonTypeRefComponent elemType)
        {
            if (elemType == null) return null;

            if (elemType.Profile.Any()) return elemType.Profile;

            var type = elemType.GetCodeFromTypeRef();
            return new[] { Canonical.ForCoreType(type).Original };
        }
    }
}
