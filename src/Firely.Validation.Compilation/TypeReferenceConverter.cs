/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
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
    public static class TypeReferenceConverter
    {
        public static IAssertion ConvertTypeReferences(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
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

        public static IAssertion ConvertTypeReference(ElementDefinition.TypeRefComponent typeRef)
        {
            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes)
            if (string.IsNullOrEmpty(typeRef.Code)) throw new IncorrectElementDefinitionException($"Encountered a typeref without a code.");

            var profiles = typeRef.Profile.ToList();

            var profileAssertions = profiles switch
            {
                // If there are no explicit profiles, use the schema associated with the declared type code in the typeref.
                { Count: 0 } => BuildSchemaAssertion(SchemaAssertion.MapTypeNameToFhirStructureDefinitionSchema(typeRef.Code)),

                // There are one or more profiles, create an "any" slice validating them 
                _ => ConvertProfilesToSchemaReferences(
                typeRef.Profile, "Element does not validate against any of the expected profiles")
            };

            //We could check this, but who cares. We'll ignore them if it is not a reference type
            //if (referenceDatatype != "canonical" && referenceDatatype != "Reference")
            //    throw new IncorrectElementDefinitionException($"Encountered targetProfiles {allowedProfiles} on an element that is not " +
            //        $"a reference type (canonical or Reference) but a {referenceDatatype}.");

            // Combine the validation against the profiles against some special cases in an "all" schema.
            if (isReferenceType(typeRef.Code))
            {
                // reference types need to start a nested validation of an instance that is referenced by uri against
                // the targetProfiles mentioned in the typeref. If there are no target profiles, then the only thing
                // we can validate against is the runtime type of the referenced resource.
                var targetProfileAssertions =
                    new AllAssertion(
                        needsRuntimeTypeCheck(typeRef.TargetProfile) ?
                            FOR_RUNTIME_TYPE
                            : ConvertProfilesToSchemaReferences(typeRef.TargetProfile, "Element does not validate against any of the expected target profiles"),
                        META_PROFILE_ASSERTION);

                var validateReferenceAssertion = buildvalidateInstance(typeRef.Code, typeRef.AggregationElement, typeRef.Versioning, targetProfileAssertions);
                return new AllAssertion(profileAssertions, validateReferenceAssertion);
            }
            else if (isExtensionType(typeRef.Code))
            {
                // Extensions need to start another validation against a schema referenced 
                // (at runtime) in the url property. Note that since the referenced profile
                // will be a profile on Extension itself, we do not need to include a default
                // profile for Extension to validate against.
                //return profiles.Any() ?
                //    new AllAssertion(profileAssertions, URL_PROFILE_ASSERTION) :
                //    URL_PROFILE_ASSERTION;
                return new AllAssertion(profileAssertions, URL_PROFILE_ASSERTION);
            }
            else if (isContainedResourceType(typeRef.Code))
            {
                // (contained) resources need to start another validation against a schema referenced 
                // (at runtime) in the meta.profile property, but also against any explicitly mentioned
                // profiles (if present). If there are no explicit profiles, or the target profile
                // is "Any" (which is actually the canonical of Resource) use the run time type of
                // the contained resource.
                return new AllAssertion(
                    needsRuntimeTypeCheck(profiles) ? FOR_RUNTIME_TYPE : profileAssertions,
                    META_PROFILE_ASSERTION);

            }
            else
                return profileAssertions;
        }

        private static bool needsRuntimeTypeCheck(IEnumerable<string> profiles) =>
            !profiles.Any() || profiles.All(p => isAnyProfile(p));

        public static readonly SchemaAssertion META_PROFILE_ASSERTION = SchemaAssertion.ForMember("meta.profile");
        public static readonly SchemaAssertion URL_PROFILE_ASSERTION = SchemaAssertion.ForMember("url");
        public static readonly SchemaAssertion FOR_RUNTIME_TYPE = SchemaAssertion.ForRuntimeType();

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
        private static IAssertion buildSliceAssertionForTypeCases(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var sliceCases = typeRefs.Select(typeRef => buildSliceForTypeCase(typeRef));

            // It should be one of the previous choices, otherwise it is an error.
            var defaultSlice = buildSliceFailure();

            return new SliceAssertion(ordered: false, defaultAtEnd: false, @default: defaultSlice, sliceCases);

            IAssertion buildSliceFailure()
            {
                var allowedCodes = string.Join(",", typeRefs.Select(t => $"'{t.Code}'"));
                return createFailure(
                    $"Element is a choice, but the instance does not use one of the allowed choice types ({allowedCodes})");
            }

            SliceAssertion.Slice buildSliceForTypeCase(ElementDefinition.TypeRefComponent typeRef)
                => new(typeRef.Code, new FhirTypeLabel(typeRef.Code), ConvertTypeReference(typeRef));
        }

        public static IAssertion BuildSchemaAssertion(string profile) => new SchemaAssertion(new Uri(profile));

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
                "canonical" => new ResourceReferenceAssertion("$this", targetSchema, convertedAgg, convertedVer),
                "Reference" => new ResourceReferenceAssertion("reference", targetSchema, convertedAgg, convertedVer),
                _ => throw new ArgumentException($"Invalid reference type {dataType}")
            };
        }

        public static IAssertion ConvertProfilesToSchemaReferences(IEnumerable<string> profiles, string failureMessagePrefix)
        {
            if (profiles is null) throw new ArgumentNullException(nameof(profiles));

            // Otherwise, build a case statement for each profile with a SchemaReference in it to the 
            // indicated profile.
            var allowedProfiles = string.Join(",", profiles.Select(p => $"'{p}'"));
            var failureMessage = $"{failureMessagePrefix}: ({allowedProfiles})";

            var cases = profiles.Select(p => (p, BuildSchemaAssertion(p)));
            return buildDiscriminatorlessChoice(cases, failureMessage);
        }

        /// <summary>
        /// This method creates a slicing, with each case as a discriminatorless match + a default that reports
        /// whatever is in the <paramref name="failureMessage"/>.
        /// </summary>        
        private static IAssertion buildDiscriminatorlessChoice(IEnumerable<(string label, IAssertion assertion)> cases, string failureMessage)
        {
            // special case 1, no cases, direct success.
            if (!cases.Any()) return ResultAssertion.SUCCESS;

            // special case 2, only one possible case, no need to build a nested
            // discriminatorless slicer to validate possible options
            if (cases.Count() == 1) return cases.Single().assertion;

            var sliceCases = cases.Select(c => buildSliceForProfile(c.label, c.assertion));

            return new SliceAssertion(ordered: false, defaultAtEnd: false, @default: createFailure(failureMessage), sliceCases);

            static SliceAssertion.Slice buildSliceForProfile(string label, IAssertion assertion) =>
                new(makeSliceName(label), assertion, ResultAssertion.SUCCESS);

            static string makeSliceName(string profile)
            {
                var sb = new StringBuilder();
                foreach (var c in profile)
                {
                    if (char.IsLetterOrDigit(c))
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }

        private static ResultAssertion createFailure(string failureMessage) =>
                ResultAssertion.CreateFailure(
                    new IssueAssertion(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE, "Location: TODO", failureMessage));
    }
}