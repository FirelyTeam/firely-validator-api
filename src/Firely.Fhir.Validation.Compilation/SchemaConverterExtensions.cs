/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using Hl7.FhirPath.Sprache;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

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
        public static List<IAssertion> Convert(
            this ElementDefinition def,
            StructureDefinition structureDefinition,
            IAsyncResourceResolver resolver,
            bool isUnconstrainedElement,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)

        {
            var elements = new List<IAssertion>();

            elements
               .MaybeAdd(BuildMaxLength(def, conversionMode))
               .MaybeAdd(BuildFixed(def, conversionMode))
               .MaybeAdd(BuildPattern(def, conversionMode))
               .MaybeAdd(BuildBinding(def, structureDefinition, conversionMode))
               .MaybeAdd(BuildMinValue(def, conversionMode))
               .MaybeAdd(BuildMaxValue(def, conversionMode))
               .MaybeAddMany(BuildFp(def, conversionMode))
               .MaybeAdd(BuildCardinality(def, conversionMode))
               .MaybeAdd(BuildElementRegEx(def, conversionMode))
               .MaybeAddMany(BuildTypeRefRegEx(def, conversionMode))
               ;

            // If this element has child constraints, then we don't need to
            // add a reference to the unconstrained base definition of the element,
            // since the snapshot generated will have added all constraints from
            // the base definition to this element...unless the typeref adds
            // additional details like profiles or targetProfiles on top of the basic
            // type.
            if (isUnconstrainedElement)
                elements.MaybeAdd(BuildContentReference(def));
#if STU3
            var hasProfileDetails = def.Type.Any(tr => !string.IsNullOrEmpty(tr.Profile) || !string.IsNullOrEmpty(tr.TargetProfile));
#else
            var hasProfileDetails = def.Type.Any(tr => tr.Profile.Any() || tr.TargetProfile.Any());
#endif
            if (isUnconstrainedElement || hasProfileDetails)
                elements.MaybeAdd(BuildTypeRefValidation(def, resolver, conversionMode));

            return elements;
        }

        // Following code has many guard-ifs which I don't want to rewrite.
#pragma warning disable IDE0046 // Convert to conditional expression

        public static BindingValidator? BuildBinding(
            ElementDefinition def,
            StructureDefinition structureDefinition,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.Binding?.ValueSet is not null ?
#if STU3
                new BindingValidator(convertSTU3Binding(def.Binding.ValueSet), convertStrength(def.Binding.Strength), true, $"{structureDefinition.Url}#{def.Path}")
#else
                new BindingValidator(def.Binding.ValueSet, convertStrength(def.Binding.Strength), true, $"{structureDefinition.Url}#{def.Path}")
#endif
                : null;

#if STU3
            static string convertSTU3Binding(DataType valueSet) =>
                valueSet switch
                {
                    FhirUri uri => uri.Value,
                    ResourceReference r => r.Reference,
                    _ => throw new IncorrectElementDefinitionException($"Encountered a STU3 Binding.ValueSet with an incorrect type.")
                };
#endif
        }

        private static BindingValidator.BindingStrength? convertStrength(BindingStrength? strength) => strength switch
        {
            BindingStrength.Required => BindingValidator.BindingStrength.Required,
            BindingStrength.Extensible => BindingValidator.BindingStrength.Extensible,
            BindingStrength.Preferred => BindingValidator.BindingStrength.Preferred,
            BindingStrength.Example => BindingValidator.BindingStrength.Example,
            _ => default,
        };

        // Adds a regex for the value for each of the typerefs in the ElementDef.Type if it has a "regex" extension on it.
        public static IEnumerable<IAssertion> BuildTypeRefRegEx(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            foreach (var type in def.Type)
                if (BuildRegex(type) is { } re) yield return re;
        }

        public static SchemaReferenceValidator? BuildContentReference(ElementDefinition def)
        {
            if (def.ContentReference is null) return null;

            // The content reference looks like http://hl7.org/fhir/StructureDefinition/Patient#Patient.x.y),
            // OR just #Patient.x.y
            if (def.ContentReference.StartsWith("#"))
            {
                var datatype = def.ContentReference[(def.ContentReference.IndexOf("#") + 1)..].Split('.')[0];
                return new SchemaReferenceValidator(Canonical.ForCoreType(datatype).Uri + def.ContentReference);
            }
            else
                return new SchemaReferenceValidator(def.ContentReference);


        }

        // Adds a regex for the value if the ElementDef has a "regex" extension on it.
        public static IAssertion? BuildElementRegEx(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return BuildRegex(def);
        }

        public static IAssertion? BuildMinValue(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.MinValue != null ? new MinMaxValueValidator(def.MinValue.ToTypedElement(), MinMaxValueValidator.ValidationMode.MinValue) : null;
        }

        public static MinMaxValueValidator? BuildMaxValue(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.MaxValue != null ? new MinMaxValueValidator(def.MaxValue.ToTypedElement(), MinMaxValueValidator.ValidationMode.MaxValue) : null;
        }

        public static FixedValidator? BuildFixed(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).

            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.Fixed != null ? new FixedValidator(def.Fixed.ToTypedElement()) : null;
        }

        public static PatternValidator? BuildPattern(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.Pattern != null ? new PatternValidator(def.Pattern.ToTypedElement()) : null;
        }

        public static IAssertion? BuildMaxLength(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.MaxLength.HasValue ? new MaxLengthValidator(def.MaxLength.Value) : null;
        }


        private static InvariantValidator? getBuiltInValidatorFor(string key) => key switch
        {
            "ele-1" => new FhirEle1Validator(),
            "ext-1" => new FhirExt1Validator(),
            _ => null
        };


        public static IEnumerable<IAssertion> BuildFp(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is part of an element (whether referring to a backbone type or not),
            // so this should not be part of the type generated for a backbone (see eld-5).
            // Note: the snapgen will ensure this constraint is copied over from the referred
            // element to the referring element (= which has a contentReference).
            if (conversionMode == ElementConversionMode.BackboneType) yield break;

            foreach (var constraint in def.Constraint)
            {
                if (getBuiltInValidatorFor(constraint.Key) is { } biv)
                    yield return biv;
                else
                {
                    var bestPractice = constraint.GetBoolExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice") ?? false;
                    var fpAssertion = new FhirPathValidator(constraint.Key, constraint.Expression, constraint.Human, convertConstraintSeverity(constraint.Severity), bestPractice);
                    yield return fpAssertion;
                }
            }

            static IssueSeverity? convertConstraintSeverity(ElementDefinition.ConstraintSeverity? constraintSeverity) => constraintSeverity switch
            {
                ElementDefinition.ConstraintSeverity.Error => IssueSeverity.Error,
                ElementDefinition.ConstraintSeverity.Warning => IssueSeverity.Warning,
                _ => default,
            };
        }

        public static IAssertion? BuildCardinality(
            ElementDefinition def,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is part of an element (whether referring to a backbone type or not),
            // so this should not be part of the type generated for a backbone (see eld-5).
            // Note: the snapgen will ensure this constraint is copied over from the referred
            // element to the referring element (= which has a contentReference).
            if (conversionMode == ElementConversionMode.BackboneType) return null;

            // Avoid generating cardinality checks on the root of resources and datatypes,
            // except for Extensions
            if (!def.Path.Contains('.') && def.Path != ModelInfo.FhirTypeToFhirTypeName(FHIRAllTypes.Extension)) return null;

            return def.Min is null && (def.Max is null || def.Max == "*") ?
                    null :
                    CardinalityValidator.FromMinMax(def.Min, def.Max);
        }

        public static RegExValidator? BuildRegex(
            IExtendable elementDef,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            var pattern =
                elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex") ?? // R4
                elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/structuredefinition-regex"); //STU3
            return pattern != null ? new RegExValidator(pattern) : null;
        }

        public static IAssertion? BuildTypeRefValidation(
            this ElementDefinition def,
            IAsyncResourceResolver resolver,
            ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) return null;

            return def.Type.Any() ?
                      new TypeReferenceConverter(resolver).ConvertTypeReferences(def.Type) :
                      null;
        }

#pragma warning restore IDE0046 // Convert to conditional expression

        public static IAssertion GroupAll(this IEnumerable<IAssertion> assertions, IValidatable? emptyAssertion = null)
        {
            // No use having simple SUCCESS Results in an all, so we can optimize.
            var optimizedList = assertions.Where(a => a != ResultAssertion.SUCCESS).ToList();

            return optimizedList switch
            {
                { Count: 0 } => emptyAssertion ?? ResultAssertion.SUCCESS,
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
