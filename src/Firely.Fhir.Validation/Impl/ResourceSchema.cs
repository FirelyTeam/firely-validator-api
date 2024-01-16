/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR Resource.
    /// </summary>
    /// <remarks>It will perform additional resource-specific validation logic associated with resources,
    /// like selecting Meta.profile as additional profiles to be validated.</remarks>
    [DataContract]
    internal class ResourceSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation structureDefinition, params IAssertion[] members) : base(structureDefinition, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation structureDefinition, IEnumerable<IAssertion> members) : base(structureDefinition, members)
        {
            // nothing
        }

        /// <summary>
        /// Gets the canonical of the profile(s) referred to in the <c>Meta.profile</c> property of the resource.
        /// </summary>
        internal static Canonical[] GetMetaProfileSchemas(IScopedNode instance, MetaProfileSelector? selector, ValidationState state)
        {
            var profiles = instance
                 .Children("meta")
                 .Children("profile")
                 .Select(ite => ite.Value)
                 .OfType<string>()
                 .Select(s => new Canonical(s));

            return callback(selector).Invoke(state.Location.InstanceLocation.ToString(), profiles.ToArray());

            static MetaProfileSelector callback(MetaProfileSelector? selector)
                => selector ?? ((_, m) => m);
        }

        /// <inheritdoc />
        internal override ResultReport ValidateInternal(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state)
        {
            // Schemas representing the root of a FHIR resource cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var results = input.Select((i, index) => ValidateInternal(i, vc, state.UpdateInstanceLocation(d => d.ToIndex(index))));
            return ResultReport.Combine(results.ToList());
        }

        /// <inheritdoc />
        internal override ResultReport ValidateInternal(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            // FHIR specific rule about dealing with abstract datatypes (not profiles!): if this schema is an abstract datatype,
            // we need to run validation against the schema for the actual type, not the abstract type.
            if (StructureDefinition.IsAbstract && StructureDefinition.Derivation != StructureDefinitionInformation.TypeDerivationRule.Constraint)
            {
                if (vc.ElementSchemaResolver is null)
                    throw new ArgumentException($"Cannot validate the resource because {nameof(ValidationSettings)} does not contain an ElementSchemaResolver.");

                var typeProfile = vc.TypeNameMapper.MapTypeName(input.InstanceType);
                var fetchResult = FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, state.UpdateLocation(d => d.InvokeSchema(this)), typeProfile);
                return fetchResult.Success ? fetchResult.Schema!.ValidateInternal(input, vc, state) : fetchResult.Error!;
            }

            // Update instance location state to start of a new Resource
            state = state.UpdateInstanceLocation(ip => ip.StartResource(input.InstanceType));

            // FHIR has a few occasions where the schema needs to read into the instance to obtain additional schemas to
            // validate against (Resource.meta.profile, Extension.url). Fetch these from the instance and combine them into
            // a coherent set to validate against.
            var additionalCanonicals = GetMetaProfileSchemas(input, vc.SelectMetaProfiles, state);

            if (additionalCanonicals.Any() && vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate profiles in meta.profile because {nameof(ValidationSettings)} does not contain an ElementSchemaResolver.");

            var additionalFetches = FhirSchemaGroupAnalyzer.FetchSchemas(vc.ElementSchemaResolver, state, additionalCanonicals);
            var fetchErrors = additionalFetches.Where(f => !f.Success).Select(f => f.Error!);

            var fetchedSchemas = additionalFetches.Where(f => f.Success).Select(f => f.Schema!).ToArray();
            var fetchedFhirSchemas = fetchedSchemas.OfType<ResourceSchema>().ToArray();
            var fetchedNonFhirSchemas = fetchedSchemas.Where(fs => fs is not ResourceSchema).ToArray();   // faster than Except

            var consistencyReport = FhirSchemaGroupAnalyzer.ValidateConsistency(null, null, fetchedFhirSchemas, state);
            var minimalSet = FhirSchemaGroupAnalyzer.CalculateMinimalSet(fetchedFhirSchemas.Append(this)).Cast<ResourceSchema>();

            // Now that we have fetched the set of most appropriate profiles, call their constraint validation -
            // this should exclude the special fetch magic for Meta.profile (this function) to avoid a loop, so we call the actual validation here.
            var validationResult = minimalSet.Select(s => s.ValidateResourceSchema(input, vc, state)).ToList();
            var validationResultOther = fetchedNonFhirSchemas.Select(s => s.ValidateInternal(input, vc, state)).ToList();
            return ResultReport.Combine(fetchErrors.Append(consistencyReport).Concat(validationResult).Concat(validationResultOther).ToArray());
        }

        /// <summary>
        /// This invokes the actual validation for an resource schema, without the special magic of 
        /// fetching Meta.profile, so this is the "normal" schema validation.
        /// </summary>
        protected ResultReport ValidateResourceSchema(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            return state.Global.RunValidations.Start(
                state,
                Id.ToString(),  // is the same as the canonical for resource schemas
                () =>
                {
                    state.Global.ResourcesValidated += 1;
                    return base.ValidateInternal(input, vc, state);
                });
        }

        /// <inheritdoc/>
        protected override string FhirSchemaKind => "resource";
    }
}
