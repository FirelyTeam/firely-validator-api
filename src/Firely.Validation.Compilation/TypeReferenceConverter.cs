/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Firely.Fhir.Validation
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

            // If there are no explicit profiles, use the schema associated with the declared type code in the typeref.
            if (!typeRef.Profile.Any())
                return BuildSchemaReference(SchemaAssertion.MapTypeNameToFhirStructureDefinitionSchema(typeRef.Code));

            // There are one or more profiles, create an "any" slice validating them 
            var profileAssertions = ConvertProfilesToSchemaReferences(
                typeRef.Profile, "Element does not validate against any of the expected profiles");

            //We could check this, but who cares. We'll ignore them if it is not a reference type
            //if (referenceDatatype != "canonical" && referenceDatatype != "Reference")
            //    throw new IncorrectElementDefinitionException($"Encountered targetProfiles {allowedProfiles} on an element that is not " +
            //        $"a reference type (canonical or Reference) but a {referenceDatatype}.");

            // Combine the validation against the profiles against some special cases in an "all" schema.
            if (isReferenceType(typeRef.Code))
            {
                // reference types need to start a nested validation of an instance that is referenced by uri against
                // the targetProfiles mentioned in the typeref.
                var targetProfileAssertions = ConvertProfilesToSchemaReferences(
                    typeRef.TargetProfile, "Element does not validate against any of the expected target profiles");

                var agg = typeRef.Aggregation?.OfType<ElementDefinition.AggregationMode>();
                var validateReferenceAssertion = buildvalidateInstance(typeRef.Code, agg, typeRef.Versioning, targetProfileAssertions);
                return new AllAssertion(profileAssertions, validateReferenceAssertion);
            }
            else if (isExtensionType(typeRef.Code))
            {
                // Extensions need to start another validation against a schema referenced 
                // (at runtime) in the url property.
                var validateExtensionTargetAssertion = new SchemaAssertion("url");
                return new AllAssertion(profileAssertions, validateExtensionTargetAssertion);
            }
            else if (isResourceType(typeRef.Code))
            {
                // (contained) resources need to start another validation against a schema referenced 
                // (at runtime) in the meta.profile property.
                var validateContainedResourceAssertion = new SchemaAssertion("meta.profile");
                return new AllAssertion(profileAssertions, validateContainedResourceAssertion);
            }
            else
                return profileAssertions;
        }

        // Note: this makes it impossible for models other than FHIR to have a reference type
        // other that types named canonical and Reference
        private static bool isReferenceType(string typeCode) => typeCode == "canonical" || typeCode == "Reference";

        private static bool isResourceType(string typeCode) => typeCode == "Resource" || typeCode == "DomainResource";

        private static bool isExtensionType(string typeCode) => typeCode == "Extension";

        /// <summary>
        /// Builds a slicing for each typeref with the FhirTypeLabel as the discriminator.
        /// </summary>
        private static IAssertion buildSliceAssertionForTypeCases(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var sliceCases = typeRefs.Select(typeRef => buildSliceForTypeCase(typeRef));

            // It should be one of the previous choices, otherwise it is an error.
            var defaultSlice = buildSliceFailure();

            return new SliceAssertion(ordered: false, @default: defaultSlice, sliceCases);

            IAssertion buildSliceFailure()
            {
                var allowedCodes = string.Join(",", typeRefs.Select(t => $"'{t.Code}'"));
                return createFailure(
                    $"Element is a choice, but the instance does not use one of the allowed choice types ({allowedCodes})");
            }

            SliceAssertion.Slice buildSliceForTypeCase(ElementDefinition.TypeRefComponent typeRef)
                => new(typeRef.Code, new FhirTypeLabel(typeRef.Code), ConvertTypeReference(typeRef));
        }

        public static IAssertion BuildSchemaReference(string profile) => new SchemaAssertion(new Uri(profile));

        /// <summary>
        /// Builds the validator that fetches a referenced resource from the runtime-supplied reference,
        /// and validates it against a targetschema + additional aggregation/versioning rules.
        /// </summary>
        private static IAssertion buildvalidateInstance(string dataType,
                           IEnumerable<ElementDefinition.AggregationMode>? agg,
                           ElementDefinition.ReferenceVersionRules? ver, IAssertion targetSchema)
        {
            var convertedAgg = agg?.Select(a => (AggregationMode)a);
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

            var cases = profiles.Select(p => (p, BuildSchemaReference(p)));
            return buildDiscriminatorlessChoice(cases, failureMessage);
        }

        /// <summary>
        /// This method creates a slicing, with each case as a discriminatorless match + a default that reports
        /// whatever is in the <paramref name="failureMessage"/>.
        /// </summary>        
        private static IAssertion buildDiscriminatorlessChoice(IEnumerable<(string label, IAssertion assertion)> cases, string failureMessage)
        {
            if (cases.Count() == 0) return ResultAssertion.SUCCESS;

            var sliceCases = cases.Select(c => buildSliceForProfile(c.label, c.assertion));

            return new SliceAssertion(ordered: false, @default: createFailure(failureMessage), sliceCases);

            SliceAssertion.Slice buildSliceForProfile(string label, IAssertion assertion) =>
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
                    new IssueAssertion(-1, failureMessage, OperationOutcome.IssueSeverity.Error));
    }
}