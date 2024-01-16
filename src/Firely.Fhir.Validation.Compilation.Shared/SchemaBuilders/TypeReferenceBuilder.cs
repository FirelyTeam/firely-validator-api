/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

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
     *     }
     *     
     *  Contained resource (Resource [Patient][]) turns into (always just 1 case in an element that represents a
     *  contained resources, never appears in choice elements):
     *  {
     *      ref: "http://hl7.org/SD/Patient"       
     *  }
     */

    /// <summary>
    /// The schema builder for type references.
    /// </summary>
    internal class TypeReferenceBuilder : ISchemaBuilder
    {
        public TypeReferenceBuilder(IAsyncResourceResolver resolver)
        {
            Resolver = resolver;
        }

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is not part of an element refering to a backbone type (see eld-5).
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;
#if STU3
            var hasProfileDetails = def.Type.Any(tr => !string.IsNullOrEmpty(tr.Profile) || !string.IsNullOrEmpty(tr.TargetProfile));
#else
            var hasProfileDetails = def.Type.Any(tr => tr.Profile.Any() || tr.TargetProfile.Any());
#endif
            if ((!nav.HasChildren || hasProfileDetails) && def.Type.Count > 0)
            {
                var typeAssertions = ConvertTypeReferences(def.Type);
                if (typeAssertions is not null)
                    yield return typeAssertions;
            }
        }

        public IAssertion? ConvertTypeReferences(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            if (!CommonTypeRefComponent.CanConvert(typeRefs))
                throw new IncorrectElementDefinitionException("Encountered an element with typerefs that cannot be converted to a common structure.");

            var r4TypeRefs = CommonTypeRefComponent.Convert(typeRefs);

            var typeRefList = r4TypeRefs.ToList();
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
                { Count: > 1 } => buildSliceAssertionForTypeCases(r4TypeRefs),

                // Just a single typeref, direct conversion.
                _ => ConvertTypeReference(r4TypeRefs.Single())
            };
        }


        internal IAssertion? ConvertTypeReference(CommonTypeRefComponent typeRef)
        {
            string code = typeRef.GetCodeFromTypeRef();

            var profileAssertions = typeRef.GetTypeProfilesCorrect()?.ToList() switch
            {
                null => null,
                // If there are no explicit profiles, use the schema associated with the declared type code in the typeref.
                { Count: 1 } single => new SchemaReferenceValidator(single.Single()),

                // There are one or more profiles, create an "any" slice validating them
                var many => ConvertProfilesToSchemaReferences(many, $"Element does not validate against any of the expected profiles ({EXPECTEDPROFILES}).")
            };

            // Combine the validation against the profiles against some special cases in an "all" schema.
            if (ReferencedInstanceValidator.IsSupportedReferenceType(code))
            {
                // reference types need to start a nested validation of an instance that is referenced by uri against
                // the targetProfiles mentioned in the typeref. If there are no target profiles, then the only thing
                // we can validate against is the runtime type of the referenced resource.
                var targetProfiles = !typeRef.TargetProfile.Any() ? new[] { Canonical.ForCoreType("Resource").ToString() } : typeRef.TargetProfile;
                var targetProfileAssertions = ConvertTargetProfilesToSchemaReferences(targetProfiles);

                var validateReferenceAssertion = buildvalidateInstance(typeRef.AggregationElement, typeRef.Versioning, targetProfileAssertions);
                return profileAssertions is not null
                    ? new AllValidator(profileAssertions, validateReferenceAssertion)
                    : validateReferenceAssertion;
            }
            else if (!(code is "Reference" or "canonical" or "CodeableReference") && typeRef.TargetProfile.Any())
            {
                throw new IncorrectElementDefinitionException($"Encountered targetProfiles {string.Join(",", typeRef.TargetProfile)} on an element that is not " +
                    $"a reference type (canonical or Reference) but a {code}.");
            }
            else
                return profileAssertions;
        }



        public IAsyncResourceResolver Resolver { get; }

        /// <summary>
        /// Builds a slicing for each typeref with the FhirTypeLabel as the discriminator.
        /// </summary>
        private IAssertion buildSliceAssertionForTypeCases(IEnumerable<CommonTypeRefComponent> typeRefs)
        {
            var sliceCases = typeRefs.Select(typeRef => buildSliceForTypeCase(typeRef));

            // It should be one of the previous choices, otherwise it is an error.
            var defaultSlice = buildSliceFailure();

            return new SliceValidator(ordered: false, defaultAtEnd: false, @default: defaultSlice, sliceCases);

            IAssertion buildSliceFailure()
            {
                var allowedCodes = string.Join(",", typeRefs.Select(t => $"'{t.Code}'"));
                return createFailure(
                    $"Element is of type '{IssueAssertion.Pattern.INSTANCETYPE}', which is not one of the allowed choice types ({allowedCodes})");
            }

            SliceValidator.SliceCase buildSliceForTypeCase(CommonTypeRefComponent typeRef)
                => new(typeRef.Code, new FhirTypeLabelValidator(typeRef.Code), ConvertTypeReference(typeRef));
        }

        /// <summary>
        /// Builds the validator that fetches a referenced resource from the runtime-supplied reference,
        /// and validates it against a targetschema + additional aggregation/versioning rules.
        /// </summary>
        private static IAssertion buildvalidateInstance(IEnumerable<Code<ElementDefinition.AggregationMode>> agg,
                           ElementDefinition.ReferenceVersionRules? ver,
                           IAssertion targetSchema)
        {
            // Convert the enum, skip nulls and make sure we use null as the
            // argument to the constructor if the collection is empty.
            var convertedAgg =
                (from a in agg
                 where a.Value is not null
                 select (AggregationMode)a.Value!)
                is { } notnullAgg && notnullAgg.Any() ? notnullAgg : null;

            var convertedVer = (ReferenceVersionRules?)ver;

            return new ReferencedInstanceValidator(targetSchema, convertedAgg, convertedVer);
        }

        private const string EXPECTEDPROFILES = "%EXPECTEDPROFILES%";

        /// <summary>
        /// Converts a list of profile urls to a "discriminatorless" slice with an error message as the default.
        /// </summary>
        /// <remarks>This effectively means that "any" match is a success, and no maches will raise the failure message.</remarks>
        public static IAssertion ConvertProfilesToSchemaReferences(IReadOnlyCollection<string> profiles, string failureMessage)
        {
            if (profiles.Count == 1) return new SchemaReferenceValidator(profiles.Single());

            var cases = profiles.Select(c => new SchemaReferenceValidator(c));
            var failure = createFailure(failureMessage, profiles);

            return new AnyValidator(cases, failure);
        }


        private record TypeChoice(string TypeLabel, string Canonical);

        public IAssertion ConvertTargetProfilesToSchemaReferences(IEnumerable<string> targetProfiles)
        {
            var typecases = targetProfiles.Select(p => new TypeChoice(fetchSd(p).Type, p)).GroupBy(pp => pp.TypeLabel).ToList();

            return buildLabelledChoice(typecases);

            StructureDefinition fetchSd(string canonical)
            {
                var sd = TaskHelper.Await(() => Resolver.FindStructureDefinitionAsync(canonical));
                return sd ?? throw new InvalidOperationException($"Compiler needs access to profile '{canonical}', but it cannot be resolved.");
            }
        }

        private static string replacep(string pattern, IEnumerable<string>? profiles) => pattern.Replace(EXPECTEDPROFILES, string.Join(", ", profiles ?? Enumerable.Empty<string>()));

        // This method creates a slicing on the instance type, where each case will then try to validate
        // on each of the profiles in the list based on that instance type.
        private static IAssertion buildLabelledChoice(List<IGrouping<string, TypeChoice>> cases)
        {
            // special case 1, no cases, direct success.
            if (!cases.Any()) return ResultAssertion.SUCCESS;

            var failureMessageA = $"Referenced resource '{IssueAssertion.Pattern.RESOURCEURL}' does not validate against any of the expected target profiles ({EXPECTEDPROFILES}).";

            // special case 2, only one possible case, no need to build a nested
            // discriminatorless slicer to validate possible options         
            if (cases.Count == 1)
            {
                var profiles = cases.Single().Select(s => s.Canonical).ToList();
                return ConvertProfilesToSchemaReferences(profiles, failureMessageA);
            }

            // case 3 - more than one type, we need to slice on type first.
            var sliceCases = cases.Select(c => buildTypeSelectorSlice(c, failureMessageA));

            var types = string.Join(", ", cases.Select(c => c.Key));
            var failureMessageB = $"{failureMessageA} None of these are profiles on type {IssueAssertion.Pattern.INSTANCETYPE} of the resource.";


            return new SliceValidator(ordered: false, defaultAtEnd: false, @default: createFailure(failureMessageB, cases.SelectMany(c => c.Select(c => c.Canonical))), sliceCases);

            static SliceValidator.SliceCase buildTypeSelectorSlice(IGrouping<string, TypeChoice> group, string failureMessage)
            {
                // The slice uses the fhir type label (in the key of the group here) as discriminator.
                var typeSelector = new FhirTypeLabelValidator(group.Key);
                return new SliceValidator.SliceCase("for" + group.Key, typeSelector, ConvertProfilesToSchemaReferences(group.Select(g => g.Canonical).ToList(), failureMessage));
            }
        }


        // TODO: there are actually two issues: one for an invalid choice, and one for a reference with an invalid targetProfile
        private static IssueAssertion createFailure(string failureMessage, IEnumerable<string>? profiles = null) =>
                    new(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE, replacep(failureMessage, profiles));
    }
}