/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using Hl7.FhirPath.Sprache;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Determines which kind of schema we want to generate from the
    /// element.
    /// </summary>
    public enum ElementConversionMode
    {
        /// <summary>
        /// Generate a schema which includes all constraints represented
        /// by the <see cref="ElementDefinition"/>.
        /// </summary>
        Full,

        /// <summary>
        /// Generate a schema which includes only those constraints that
        /// are part of the type defined inline by the backbone.
        /// </summary>
        /// <remarks>According to constraint eld-5, the ElementDefinition
        /// members type, defaultValue, fixed, pattern, example, minValue, 
        /// maxValue, maxLength, or binding cannot appear in a 
        /// <see cref="ElementDefinition.ContentReference"/>, so these are
        /// generated part of the inline-defined backbone type, not as part
        /// of the element refering to the backbone type.</remarks>
        BackboneType,

        /// <summary>
        /// Generate a schema for an element that uses a backbone type.
        /// </summary>
        /// <remarks>Note: in our schema's there is no difference in treatment
        /// between the element that defines a backbone, and those that refer
        /// to a backbone using <see cref="ElementDefinition.ContentReference"/>.
        /// The type defined inline by the backbone is extracted and both elements
        /// will refer to it, as if both had a content reference."/></remarks>
        ContentReference
    }

    internal static class SchemaConverterExtensions
    {
        public static IAssertion GroupAll(this IEnumerable<IAssertion> assertions)
        {
            // No use having simple SUCCESS Results in an all, so we can optimize.
            var optimizedList = assertions.Where(a => a != ResultAssertion.SUCCESS).ToList();

            return optimizedList switch
            {
                { Count: 0 } => ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list => new AllValidator(list, shortcircuitEvaluation: true)
            };
        }

        public static IValidatable GroupAny(this IEnumerable<IValidatable> assertions, IValidatable? emptyAssertion = null)
        {
            var listOfAssertions = assertions.ToList();

            return listOfAssertions switch
            {
                { Count: 0 } => emptyAssertion ?? ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list when list.Any(a => a.IsAlways(ValidationResult.Success)) => ResultAssertion.SUCCESS,
                var list => new AnyValidator(list)
            };
        }

        public static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, IAssertion? element)
        {
            if (element is not null)
                assertions.Add(element);

            return assertions;
        }

        public static List<IAssertion> MaybeAddMany(this List<IAssertion> assertions, IEnumerable<IAssertion> element)
        {
            assertions.AddRange(element);
            return assertions;
        }

        internal const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        internal const string SDXMLTYPEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type";

        // TODO: This would probably be useful for the SDK too
        internal static string GetCodeFromTypeRef(this CommonTypeRefComponent typeRef)
        {
            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes),
            // and there are some R4 profiles in the wild that still use this old schema too.
            if (string.IsNullOrEmpty(typeRef.Code))
            {
                var r3TypeIndicator = typeRef.CodeElement.GetStringExtension(SDXMLTYPEEXTENSION);
                if (r3TypeIndicator is null)
                    throw new IncorrectElementDefinitionException($"Encountered a typeref without a code.");

                return deriveSystemTypeFromXsdType(r3TypeIndicator);
            }
            else
                return typeRef.Code;

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
                });

                static string makeSystemType(string name) => SYSTEMTYPEURI + name;
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
