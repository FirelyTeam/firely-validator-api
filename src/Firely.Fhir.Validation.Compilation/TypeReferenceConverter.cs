/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Firely.Fhir.Validation.Compilation
{
    /* Turns a TypeRef element into a set of assertions according to this general plan:
     *
     * Identifier:[][]
     * HumanName:[HumanNameDE,HumanNameBE]:[]
     * Reference:[WithReqDefinition,WithIdentifier]:[Practitioner,OrganizationBE]
     * Extension: [ExtensionNL]
     *  switch
     *     case [TypeLabel Identifier] {
     *          ref: "http://hl7.org/SD/Identifier"
     *     },
     *     case [TypeLabel HumanName] {
     *          Any { ref: "HumanNameDE", ref: "HumanNameBE" }
     *     },
     *     case [TypeLabel Reference] {
     *          Any { ref: "WithReqDefinition", ref: "WithIdentifier" }
     *          validate: resolve-from("reference") against
     *                  Any { ref: http://hl7.org/fhir/SD/Practitioner, ref: http://example.org/OrganizationBE }
     *     }
     *     case [TypeLabel Extension] {
     *         ref: "http://hl7.org/SD/Extension"
     *         ref: runtime via "url" property
     *     }
     *     
     *  Contained resource (Resource [Patient][]) turns into (always just 1 case in an element that represents a
     *  contained resources, never appears in choice elements):
     *  {
     *      ref: "http://hl7.org/SD/Patient"       
     *      ref: runtime via "meta.profile" property
     *  }
     */
    internal class TypeReferenceConverter
    {
        public TypeReferenceConverter(IAsyncResourceResolver resolver)
        {
            Resolver = resolver;
        }

        public IAssertion ConvertTypeReferences(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var typeRefList = typeRefs.ToList();
            bool hasDuplicateCodes() => typeRefList.Select(t => t.Code).Distinct().Count() != typeRefList.Count;

            return typeRefList switch
            {
                // No type refs -> always successful
                { Count: 0 } => ResultAssertion.SUCCESS,

                // In R4, all Codes must be unique (in R3, this was seen as an OR)
                _ when hasDuplicateCodes() => throw new IncorrectElementDefinitionException($"Encountered an element with typerefs with non-unique codes."),

                // More than one type.Code => build a switch based on the instance's type so we get
                // useful error messages, and can continue structural validation once we have determined
                // the correct type.
                { Count: > 1 } => buildSliceAssertionForTypeCases(typeRefs),

                // Just a single typeref, direct conversion.
                _ => ConvertTypeReference(typeRefs.Single())
            };
        }

        private const string SYSTEMTYPEURI = "http://hl7.org/fhirpath/System.";
        public const string SDXMLTYPEEXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-xml-type";
        private static string makeSystemType(string name) => SYSTEMTYPEURI + name;

        private static string deriveSystemTypeFromXsdType(string xsdTypeName)
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
        }

        public IAssertion ConvertTypeReference(ElementDefinition.TypeRefComponent typeRef)
        {
            string code;

            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes),
            // and there are some R4 profiles in the wild that still use this old schema too.
            if (string.IsNullOrEmpty(typeRef.Code))
            {
                var r3TypeIndicator = typeRef.CodeElement.GetStringExtension(SDXMLTYPEEXTENSION);
                if (r3TypeIndicator is null)
                    throw new IncorrectElementDefinitionException($"Encountered a typeref without a code.");

                code = deriveSystemTypeFromXsdType(r3TypeIndicator);
            }
            else
                code = typeRef.Code;

            var profiles = typeRef.Profile.ToList();
            var profileAssertions = profiles switch
            {
                // If there are no explicit profiles, use the schema associated with the declared type code in the typeref.
                { Count: 0 } => BuildSchemaAssertion(RuntimeTypeValidator.MapTypeNameToFhirStructureDefinitionSchema(code)),

                // There are one or more profiles, create an "any" slice validating them 
                _ => ConvertProfilesToSchemaReferences(
                typeRef.Profile, "Element does not validate against any of the expected profiles")
            };

            //We could check this, but who cares. We'll ignore them if it is not a reference type
            //if (referenceDatatype != "canonical" && referenceDatatype != "Reference")
            //    throw new IncorrectElementDefinitionException($"Encountered targetProfiles {allowedProfiles} on an element that is not " +
            //        $"a reference type (canonical or Reference) but a {referenceDatatype}.");

            // Combine the validation against the profiles against some special cases in an "all" schema.
            if (isReferenceType(code))
            {
                // reference types need to start a nested validation of an instance that is referenced by uri against
                // the targetProfiles mentioned in the typeref. If there are no target profiles, then the only thing
                // we can validate against is the runtime type of the referenced resource.
                var targetProfileAssertions =
                    new AllValidator(
                        needsRuntimeTypeCheck(typeRef.TargetProfile) ?
                            FOR_RUNTIME_TYPE
                            : ConvertProfilesToSchemaReferences(typeRef.TargetProfile, "Element does not validate against any of the expected target profiles"),
                        META_PROFILE_ASSERTION);

                var validateReferenceAssertion = buildvalidateInstance(code, typeRef.AggregationElement, typeRef.Versioning, targetProfileAssertions);
                return new AllValidator(profileAssertions, validateReferenceAssertion);
            }
            else if (isExtensionType(code))
            {
                // Extensions need to start another validation against a schema referenced 
                // (at runtime) in the url property. Note that since the referenced profile
                // will be a profile on Extension itself, we do not need to include a default
                // profile for Extension to validate against.
                //return profiles.Any() ?
                //    new AllAssertion(profileAssertions, URL_PROFILE_ASSERTION) :
                //    URL_PROFILE_ASSERTION;
                var additionalProfiles = profiles.Select(p => new Canonical(p)).ToArray();
                var extensionValidator = new DynamicSchemaReferenceValidator("url", ResourceIdentity.Core("Extension").ToString(), additionalProfiles);
                //return profiles.Any() ? new AllValidator(profileAssertions, URL_PROFILE_ASSERTION) : URL_PROFILE_ASSERTION;
                return extensionValidator;
            }

            else if (isContainedResourceType(code))
            {
                // (contained) resources need to start another validation against a schema referenced 
                // (at runtime) in the meta.profile property, but also against any explicitly mentioned
                // profiles (if present). If there are no explicit profiles, or the target profile
                // is "Any" (which is actually the canonical of Resource) use the run time type of
                // the contained resource.
                return new AllValidator(
                    needsRuntimeTypeCheck(profiles) ? FOR_RUNTIME_TYPE : profileAssertions,
                    META_PROFILE_ASSERTION);

            }
            else
                return profileAssertions;
        }

        private static bool needsRuntimeTypeCheck(IEnumerable<string> profiles) =>
            !profiles.Any() || profiles.All(p => isAnyProfile(p));

        public static readonly DynamicSchemaReferenceValidator META_PROFILE_ASSERTION = new("meta.profile");
        public static readonly DynamicSchemaReferenceValidator URL_PROFILE_ASSERTION = new("url");
        public static readonly RuntimeTypeValidator FOR_RUNTIME_TYPE = new();

        public IAsyncResourceResolver Resolver { get; }

        // Note: this makes it impossible for models other than FHIR to have a reference type
        // other that types named canonical and Reference
        //private static bool isReferenceType(string typeCode) => typeCode == "canonical" || typeCode == "Reference";
        private static bool isReferenceType(string typeCode) => typeCode == "Reference";

        private static bool isContainedResourceType(string typeCode) => typeCode == "Resource" || typeCode == "DomainResource";

        private static bool isAnyProfile(string uri) => uri == "http://hl7.org/fhir/StructureDefinition/Resource";

        private static bool isExtensionType(string typeCode) => typeCode == "Extension";

        /// <summary>
        /// Builds a slicing for each typeref with the FhirTypeLabel as the discriminator.
        /// </summary>
        private IAssertion buildSliceAssertionForTypeCases(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var sliceCases = typeRefs.Select(typeRef => buildSliceForTypeCase(typeRef));

            // It should be one of the previous choices, otherwise it is an error.
            var defaultSlice = buildSliceFailure();

            return new SliceValidator(ordered: false, defaultAtEnd: false, @default: defaultSlice, sliceCases);

            IAssertion buildSliceFailure()
            {
                var allowedCodes = string.Join(",", typeRefs.Select(t => $"'{t.Code}'"));
                return createFailure(
                    $"Element is a choice, but the instance does not use one of the allowed choice types ({allowedCodes})");
            }

            SliceValidator.SliceCase buildSliceForTypeCase(ElementDefinition.TypeRefComponent typeRef)
                => new(typeRef.Code, new FhirTypeLabelValidator(typeRef.Code), ConvertTypeReference(typeRef));
        }

        public static IAssertion BuildSchemaAssertion(Canonical profile) => new SchemaReferenceValidator(profile);

        /// <summary>
        /// Builds the validator that fetches a referenced resource from the runtime-supplied reference,
        /// and validates it against a targetschema + additional aggregation/versioning rules.
        /// </summary>
        private static IAssertion buildvalidateInstance(string dataType,
                           IEnumerable<Code<ElementDefinition.AggregationMode>> agg,
                           ElementDefinition.ReferenceVersionRules? ver, IAssertion targetSchema)
        {
            // Convert the enum, skip nulls and make sure we use null as the
            // argument to the constructor if the collection is empty.
            var convertedAgg =
                (from a in agg
                 where a.Value is not null
                 select (AggregationMode)a.Value!)
                is { } notnullAgg && notnullAgg.Any() ? notnullAgg : null;

            var convertedVer = (ReferenceVersionRules?)ver;

            return dataType switch
            {
                "canonical" => new ReferencedInstanceValidator("$this", targetSchema, convertedAgg, convertedVer),
                "Reference" => new ReferencedInstanceValidator("reference", targetSchema, convertedAgg, convertedVer),
                _ => throw new ArgumentException($"Invalid reference type {dataType}")
            };
        }

        private record TypeChoice(string TypeLabel, string Canonical, IAssertion SchemaAssertion);

        public IAssertion ConvertProfilesToSchemaReferences(IEnumerable<string> profiles, string failureMessagePrefix)
        {
            var typecases = profiles.Select(p => new TypeChoice(fetchSd(p).Type, p, BuildSchemaAssertion(p))).GroupBy(pp => pp.TypeLabel).ToList();

            return buildLabelledChoice(typecases, failureMessagePrefix);

            StructureDefinition fetchSd(string canonical)
            {
                var sd = TaskHelper.Await(() => Resolver.FindStructureDefinitionAsync(canonical));
                return sd ?? throw new InvalidOperationException($"Compiler needs access to profile '{canonical}', but it cannot be resolved.");
            }
        }

        // This method creates a slicing on the instance type, where each case will then try to validate
        // on each of the profiles in the list based on that instance type.
        private static IAssertion buildLabelledChoice(List<IGrouping<string, TypeChoice>> cases, string failureMessagePrefix)
        {
            // special case 1, no cases, direct success.
            if (!cases.Any()) return ResultAssertion.SUCCESS;

            // special case 2, only one possible case, no need to build a nested
            // discriminatorless slicer to validate possible options
            if (cases.Count == 1) return buildSlicerForProfiles(cases.Single().ToList(), failureMessagePrefix);

            //var profiles = string.Join(',', cases.SelectMany(c => c.Select(c => c.Canonical)));
            var types = string.Join(", ", cases.Select(c => c.Key));

            var failureMessage = $"{failureMessagePrefix}: based on these profiles, the instance type should have been one of ({types}).";

            var sliceCases = cases.Select(c => buildTypeSelectorSlice(c, failureMessagePrefix));

            return new SliceValidator(ordered: false, defaultAtEnd: false, @default: createFailure(failureMessage), sliceCases);
        }

        private static SliceValidator.SliceCase buildTypeSelectorSlice(IGrouping<string, TypeChoice> group, string failureMessagePrefix)
        {
            // The slice uses the fhir type label (in the key of the group here) as discriminator.
            var typeSelector = new FhirTypeLabelValidator(group.Key);
            return new SliceValidator.SliceCase("for" + group.Key, typeSelector, buildSlicerForProfiles(group.ToList(), failureMessagePrefix));
        }

        private static IAssertion buildSlicerForProfiles(List<TypeChoice> choices, string failureMessagePrefix)
        {
            if (choices.Count == 1) return choices.Single().SchemaAssertion;

            var cases = choices.Select(c => new SliceValidator.SliceCase(makeSliceName(c.Canonical), c.SchemaAssertion, ResultAssertion.SUCCESS));

            var profiles = string.Join(", ", choices.Select(c => c.Canonical));
            var failureMessage = $"{failureMessagePrefix}: {profiles}.";

            return new SliceValidator(ordered: false, defaultAtEnd: false, @default: createFailure(failureMessage), cases);
        }

        //new(makeSliceName(label), assertion, ResultAssertion.SUCCESS);

        private static string makeSliceName(string profile)
        {
            var sb = new StringBuilder();
            foreach (var c in profile)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // TODO: there are actually two issues: one for an invalid choice, and one for a reference with an invalid targetProfile
        private static IAssertion createFailure(string failureMessage) =>
                    new IssueAssertion(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE, null, failureMessage);
    }
}