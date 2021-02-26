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
     * 
     * Any
     * {
     *     switch
     *     [TypeLabel Identifier] {
     *          ref: "http://hl7.org/SD/Identifier"
     *     },
     *     [TypeLabel HumanName]
     *     {
     *          Any { ref: "HumanNameDE", ref: "HumanNameBE" }
     *     },
     *     [TypeLabel Reference]
     *     {
     *          Any { ref: "WithReqDefinition", ref: "WithIdentifier" }
     *          validate: resolve-from("reference") against
     *                  Any { ref: http://hl7.org/fhir/SD/Practitioner, ref: http://example.org/OrganizationBE }
     *     }
     * }
     */
    public static class TypeReferenceConverter
    {
        public static IAssertion? ConvertTypeReferences(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs, string path)
        {
            if (!typeRefs.Any()) return null;

            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes)
            if (typeRefs.Any(t => string.IsNullOrEmpty(t.Code)))
                throw new IncorrectElementDefinitionException($"Encountered a typeref without a code at {path}");

            // In R4, all Codes must be unique (in R3, this was seen as an OR)
            if (typeRefs.Select(t => t.Code).Distinct().Count() != typeRefs.Count())
                throw new IncorrectElementDefinitionException($"Encountered an element with typerefs with non-unique codes at {path}");

            if (typeRefs.Count() > 1)
            {
                // More than one type.Code => build a switch based on the instance's type so we get
                // useful error messages, and can continue structural validation once we have determined
                // the correct type.
                return buildSliceAssertionForTypeCases(typeRefs);
            }
            else
            {
                return ConvertTypeReference(typeRefs.Single());
            }
        }


        public static IAssertion ConvertTypeReference(ElementDefinition.TypeRefComponent typeRef)
        {
            // This created the validation for the *profiles* mentioned in the typeref.
            var profileAssertions = ConvertTypeReferenceProfiles(typeRef.Code, typeRef.Profile);

            //We could check this, but who cares. We'll ignore them if it is not a reference type
            //if (referenceDatatype != "canonical" && referenceDatatype != "Reference")
            //    throw new IncorrectElementDefinitionException($"Encountered targetProfiles {allowedProfiles} on an element that is not " +
            //        $"a reference type (canonical or Reference) but a {referenceDatatype}.");

            if (isReferenceType(typeRef.Code))
            {
                var targetProfileAssertions = ConvertTargetProfiles(typeRef.Profile);

                var agg = typeRef.Aggregation?.OfType<ElementDefinition.AggregationMode>();
                var validateReferenceAssertion = buildvalidateInstance(typeRef.Code, agg, typeRef.Versioning, targetProfileAssertions);
                return new AllAssertion(profileAssertions, targetProfileAssertions);
            }
            else
                return profileAssertions;
        }

        // Note: this makes it impossible for models other than FHIR to have a reference type
        // other that types named canonical and Reference
        private static bool isReferenceType(string typeCode) => typeCode == "canonical" || typeCode == "Reference";

        private static IAssertion buildSliceAssertionForTypeCases(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var sliceCases = typeRefs.Select(typeRef => buildSliceForTypeCase(typeRef));

            // It should be one of the previous choices, otherwise it is an error.
            var defaultSlice = buildSliceFailure();

            return new SliceAssertion(ordered: false, @default: defaultSlice, sliceCases);

            IAssertion buildSliceFailure()
            {
                var allowedCodes = string.Join(",", typeRefs.Select(t => $"'{t.Code}'"));
                return
                    ResultAssertion.CreateFailure(
                        new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", $"Element is a choice, but the instance does not use one of the allowed choice types ({allowedCodes})"));
            }

            SliceAssertion.Slice buildSliceForTypeCase(ElementDefinition.TypeRefComponent typeRef)
                => new SliceAssertion.Slice(typeRef.Code, new FhirTypeLabel(typeRef.Code), ConvertTypeReference(typeRef));
        }

        public static IAssertion BuildSchemaReference(string profile) => new SchemaReferenceAssertion(new Uri(profile));

        private static IAssertion buildvalidateInstance(string dataType,
                           IEnumerable<ElementDefinition.AggregationMode>? agg,
                           ElementDefinition.ReferenceVersionRules? ver, IAssertion targetSchema)
        {
            var convertedAgg = agg?.Select(a => (AggregationMode)a);
            var convertedVer = (ReferenceVersionRules?)ver;

            return dataType switch
            {
                "canonical" => new ValidateReferencedInstanceAssertion("$this", targetSchema, convertedAgg, convertedVer),
                "Reference" => new ValidateReferencedInstanceAssertion("reference", targetSchema, convertedAgg, convertedVer),
                _ => throw new ArgumentException($"Invalid reference type {dataType}")
            };
        }

        public static IAssertion ConvertTypeReferenceProfiles(string code, IEnumerable<string> profiles)
        {
            // If there are no explicit profiles, use the schema associated with the declared type code in the typeref.
            if (!profiles.Any())
                return BuildSchemaReference(SchemaReferenceAssertion.MapTypeNameToFhirStructureDefinitionSchema(code));

            var allowedProfiles = string.Join(",", profiles.Select(p => $"'{p}'"));
            var failureMessage = $"Element does not validate against any of the expected profiles ({allowedProfiles})";
            return buildDiscriminatorlessChoice(profiles.Select(p => (p, BuildSchemaReference(p))), failureMessage);
        }

        public static IAssertion ConvertTargetProfiles(IEnumerable<string> targetProfiles)
        {
            // If there are no target profiles, we must validate against the runtime type of the 
            // target, which we can find out using it's InstanceType property.
            if (!targetProfiles.Any())
                return SchemaReferenceAssertion.ForRuntimeType();

            // Otherwise, build a case statement for each targetprofile with a SchemaReference in it to the 
            // indicated targetProfile
            var allowedProfiles = string.Join(",", targetProfiles.Select(p => $"'{p}'"));

            var failureMessage = $"Element does not validate against any of the expected target profiles ({allowedProfiles})";
            var cases = targetProfiles.Select(p => (p, (IAssertion)new SchemaReferenceAssertion(new Uri(p))));
            return buildDiscriminatorlessChoice(cases, failureMessage);
        }

        private static IAssertion buildDiscriminatorlessChoice(IEnumerable<(string label, IAssertion assertion)> cases, string failureMessage)
        {
            // "special" case, only one possible case, no need to build a nested
            // discriminatorless slicer to validate possible options
            if (cases.Count() == 1) return cases.Single().assertion;

            var sliceCases = cases.Select(c => buildSliceForProfile(c.label, c.assertion));

            return new SliceAssertion(ordered: false, @default: buildSliceFailure(), sliceCases);

            IAssertion buildSliceFailure() =>
                ResultAssertion.CreateFailure(
                    new IssueAssertion(-1, failureMessage, IssueSeverity.Error));

            SliceAssertion.Slice buildSliceForProfile(string label, IAssertion assertion) =>
                new SliceAssertion.Slice(makeSliceName(label), assertion, ResultAssertion.SUCCESS);

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
    }
}