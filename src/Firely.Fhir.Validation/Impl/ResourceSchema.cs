/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR Resource.
    /// </summary>
    /// <remarks>It will perform additional resource-specific validation logic associated with resources,
    /// like selecting Meta.profile as additional profiles to be validated.</remarks>
    public class ResourceSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation sdi, params IAssertion[] members) : base(sdi, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation sdi, IEnumerable<IAssertion> members) : base(sdi, members)
        {
            // nothing
        }

        /// <summary>
        /// Gets the canonical of the profile(s) referred to in the <c>Meta.profile</c> property of the resource.
        /// </summary>
        public static Canonical[] GetMetaProfileSchemas(ITypedElement instance) =>
            instance
                .Children("meta")
                .Children("profile")
                .Select(ite => ite.Value)
                .OfType<string>()
                .Select(s => new Canonical(s))
                .ToArray();

        /// <inheritdoc />
        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            // Schemas representing the root of a FHIR resource cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var results = input.Select(i => Validate(i, vc, state));
            return ResultReport.FromEvidence(results.ToList());
        }

        /// <inheritdoc />
        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            // FHIR specific rule about dealing with abstract datatypes (not profiles!): if this schema is an abstract datatype,
            // we need to run validation against the schema for the actual type, not the abstract type.
            if (StructureDefinition.IsAbstract && StructureDefinition.Derivation != StructureDefinitionInformation.TypeDerivationRule.Constraint)
            {
                if (vc.ElementSchemaResolver is null)
                    throw new ArgumentException($"Cannot validate the resource because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

                var typeProfile = Canonical.ForCoreType(input.InstanceType);
                var fetchResult = FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, input.Location, typeProfile);
                return fetchResult.Success ? fetchResult.Schema!.Validate(input, vc, state) : fetchResult.Error!;
            }

            // FHIR has a few occasions where the schema needs to read into the instance to obtain additional schemas to
            // validate against (Resource.meta.profile, Extension.url). Fetch these from the instance and combine them into
            // a coherent set to validate against.
            var additionalCanonicals = GetMetaProfileSchemas(input);

            if (additionalCanonicals.Any() && vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate profiles in meta.profile because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            var additionalFetches = FhirSchemaGroupAnalyzer.FetchSchemas(vc.ElementSchemaResolver, input.Location, additionalCanonicals);
            var fetchErrors = additionalFetches.Where(f => !f.Success).Select(f => f.Error!);

            var fetchedSchemas = additionalFetches.Where(f => f.Success).Select(f => f.Schema!).ToArray();
            var fetchedFhirSchemas = fetchedSchemas.OfType<ResourceSchema>().ToArray();
            var fetchedNonFhirSchemas = fetchedSchemas.Where(fs => fs is not ResourceSchema).ToArray();   // faster than Except

            var consistencyReport = FhirSchemaGroupAnalyzer.ValidateConsistency(null, null, fetchedFhirSchemas, input.Location);
            var minimalSet = FhirSchemaGroupAnalyzer.CalculateMinimalSet(fetchedFhirSchemas.Append(this)).Cast<ResourceSchema>();

            // Now that we have fetched the set of most appropriate profiles, call their constraint validation -
            // this should exclude the special fetch magic for Meta.profile (this function) to avoid a loop, so we call the actual validation here.
            var validationResult = minimalSet.Select(s => s.ValidateResourceSchema(input, vc, state)).ToList();
            var validationResultOther = fetchedNonFhirSchemas.Select(s => s.Validate(input, vc, state)).ToList();
            return ResultReport.FromEvidence(fetchErrors.Append(consistencyReport).Concat(validationResult).Concat(validationResultOther).ToArray());
        }

        /// <summary>
        /// This invokes the actual validation for an resource schema, without the special magic of 
        /// fetching Meta.profile, so this is the "normal" schema validation.
        /// </summary>
        protected ResultReport ValidateResourceSchema(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var resourceUrl = state.Instance.ResourceUrl;
            var fullLocation = (resourceUrl is not null ? resourceUrl + "#" : "") + input.Location;

            return state.Global.RunValidations.Start(
                fullLocation,
                Id.ToString(),  // is the same as the canonical for resource schemas
                () =>
                {
                    state.Global.ResourcesValidated += 1;
                    return base.Validate(input, vc, state);
                });
        }

        /// <inheritdoc/>
        protected override string FhirSchemaKind => "resource";
    }
}
