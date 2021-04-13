/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Fetches an instance by reference and starts validation against a schema. The reference is 
    /// to be found at runtime via the input, at the child member specified by <see cref="ReferenceUriMember"/>.
    /// </summary>
    [DataContract]
    public class ReferencedInstanceValidator : IValidatable
    {

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string ReferenceUriMember { get; private set; }

        [DataMember(Order = 1)]
        public IElementSchema Schema { get; private set; }

        [DataMember(Order = 2)]
        public IEnumerable<AggregationMode>? AggregationRules { get; private set; }

        [DataMember(Order = 3]
        public ReferenceVersionRules? VersioningRules { get; private set; }
#else
        /// <summary>
        /// The path to the member within the instance where the reference to be
        /// resolved can be found.
        /// </summary>
        [DataMember]
        public string ReferenceUriMember { get; private set; }

        /// <summary>
        /// When the referenced resource was found, it will be validated against
        /// this schema.
        /// </summary>
        [DataMember]
        public IAssertion Schema { get; private set; }

        /// <summary>
        /// Additional rules about the context of the referenced resource.
        /// </summary>
        [DataMember]
        public IEnumerable<AggregationMode>? AggregationRules { get; private set; }

        /// <summary>
        /// Additional rules about versioning of the reference.
        /// </summary>
        [DataMember]
        public ReferenceVersionRules? VersioningRules { get; private set; }
#endif


        /// <summary>
        /// Create a <see cref="ReferencedInstanceValidator"/>.
        /// </summary>
        /// <param name="referenceUriMember"><inheritdoc cref="ReferenceUriMember"/></param>
        public ReferencedInstanceValidator(string referenceUriMember, IAssertion schema,
            IEnumerable<AggregationMode>? aggregationRules = null, ReferenceVersionRules? versioningRules = null)
        {
            ReferenceUriMember = referenceUriMember ?? throw new ArgumentNullException(nameof(referenceUriMember));
            Schema = schema ?? throw new ArgumentNullException(nameof(referenceUriMember));
            AggregationRules = aggregationRules?.ToArray();
            VersioningRules = versioningRules;
        }

        /// <summary>
        /// Whether any <see cref="AggregationRules"/> have been specified on the constructor.
        /// </summary>
        public bool HasAggregation => AggregationRules?.Any() ?? false;

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext)"/>
        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            // Get the actual reference from the instance by the pre-configured name.
            // The name is usually "reference" in case we are dealing with a FHIR reference type,
            // or "$this" if the input is a canonical (which is primitive).  This may of course
            // be different for different modelling paradigms.
            var reference = SchemaReferenceValidator.GetStringByMemberName(input, ReferenceUriMember);

            // It's ok for a reference to have no value (but, say, a description instead),
            // so only go out to fetch the reference if we have one.
            if (reference is not null)
            {
                if (state.Visited(input.Location, reference)) // The validator already visited this instance
                {
                    return new Assertions(new ResultAssertion(ValidationResult.Failure,
                        new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE, null,
                        $"Detected a circular reference on location {input.Location} for reference {reference}")));
                }

                state = state.AddReferenceState(input.Location, reference);

                // Try to fetch the reference, which will also validate the aggregation/versioning rules etc.
                var (assertions, resolution) = await fetchReference(input, reference, vc).ConfigureAwait(false);

                // If the reference was resolved (either internally or externally), validate it
                return resolution.ReferencedResource switch
                {
                    null => assertions + ResultAssertion.CreateFailure(new IssueAssertion(
                        Issue.UNAVAILABLE_REFERENCED_RESOURCE, input.Location,
                        $"Cannot resolve reference {reference}")),
                    _ => assertions + await validateReferencedResource(vc, resolution, state).ConfigureAwait(false)
                };
            }
            else
                return Assertions.SUCCESS;
        }

        private record ResolutionResult(ITypedElement? ReferencedResource, AggregationMode? ReferenceKind, ReferenceVersionRules? VersioningKind);

        /// <summary>
        /// Try to fetch the referenced resource. The resource may be present in the instance (bundled, contained)
        /// or externally. In the last case, the <see cref="ValidationContext.ExternalReferenceResolver"/> is used
        /// to fetch the resource.
        /// </summary>
        private async Task<(Assertions, ResolutionResult)> fetchReference(ITypedElement input, string reference, ValidationContext vc)
        {
            ResolutionResult resolution = new(null, null, null);
            var assertions = Assertions.EMPTY;

            if (input is not ScopedNode instance)
                throw new InvalidOperationException($"Cannot validate because input is not of type {nameof(ScopedNode)}.");

            // First, try to resolve within this instance (in contained, Bundle.entry)
            assertions += resolveLocally(instance, reference, out resolution);

            // Now that we have tried to fetch the reference locally, we have also determined the kind of
            // reference we are dealing with, so check it for aggregation and versioning rules.
            if (HasAggregation && !AggregationRules.Any(a => a == resolution.ReferenceKind))
            {
                var allowed = string.Join(", ", AggregationRules);
                assertions += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND, input.Location,
                    $"Encountered a reference ({reference}) of kind '{resolution.ReferenceKind}', which is not one of the allowed kinds ({allowed})."));
            }

            if (VersioningRules is not null && VersioningRules != ReferenceVersionRules.Either)
            {
                if (VersioningRules != resolution.VersioningKind)
                    assertions += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND, input.Location,
                        $"Expected a {VersioningRules} versioned reference but found {resolution.VersioningKind}."));
            }

            if (resolution.ReferenceKind == AggregationMode.Referenced)
            {
                // Bail out if we are asked to follow an *external reference* when this is disabled in the settings
                if (vc.ExternalReferenceResolver is null)
                    return (assertions, resolution);

                // If we are supposed to resolve the reference externally, then do so now.
                if (resolution.ReferencedResource is null)
                {
                    try
                    {
                        var externalReference = await vc.ExternalReferenceResolver!(reference).ConfigureAwait(false);
                        resolution = resolution with { ReferencedResource = externalReference };
                    }
                    catch (Exception e)
                    {
                        assertions += ResultAssertion.CreateFailure(new IssueAssertion(
                            Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance.Location,
                            $"Resolution of external reference {reference} failed. Message: {e.Message}"));
                    }
                }
            }

            return (assertions, resolution);
        }

        /// <summary>
        /// Try to fetch the resource within this instance (e.g. a contained or bundled resource).
        /// </summary>
        private static Assertions resolveLocally(ScopedNode instance, string reference, out ResolutionResult resolution)
        {
            resolution = new ResolutionResult(null, null, null);
            var identity = new ResourceIdentity(reference);

            var (url, version) = splitCanonical(reference);
            resolution = resolution with { VersioningKind = version is not null ? ReferenceVersionRules.Specific : ReferenceVersionRules.Independent };

            if (identity.Form == ResourceIdentityForm.Undetermined)
            {
                if (!Uri.IsWellFormedUriString(Uri.EscapeDataString(reference), UriKind.RelativeOrAbsolute))
                {
                    return new Assertions(ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_UNPARSEABLE_REFERENCE, instance.Location,
                        $"Encountered an unparseable reference ({reference}")));
                }
            }

            var referencedResource = instance.Resolve(reference);

            resolution = identity.Form switch
            {
                ResourceIdentityForm.Local =>
                    resolution with
                    {
                        ReferenceKind = AggregationMode.Contained,
                        ReferencedResource = referencedResource
                    },
                _ =>
                    resolution with
                    {
                        ReferenceKind = referencedResource is not null ?
                            AggregationMode.Bundled : AggregationMode.Referenced,
                        ReferencedResource = referencedResource
                    }
            };

            return Assertions.EMPTY;
        }

        /// <summary>
        /// Validate the referenced resource against the <see cref="Schema"/>.
        /// </summary>
        private async Task<Assertions> validateReferencedResource(ValidationContext vc, ResolutionResult resolution, ValidationState state)
        {
            if (resolution.ReferencedResource is null) throw new ArgumentException("Resolution should have a non-null referenced resource by now.");
            var result = Assertions.EMPTY;

            //result += Trace($"Starting validation of referenced resource {reference} ({encounteredKind})");

            // References within the instance are dealt with within the same validator,
            // references to external entities will operate within a new instance of a validator (and hence a new tracking context).
            // In both cases, the outcome is included in the result.
            return resolution.ReferenceKind != AggregationMode.Referenced
                ? result + await Schema.Validate(resolution.ReferencedResource, vc, state).ConfigureAwait(false)
                : result + await Schema.Validate(new ScopedNode(resolution.ReferencedResource), vc, state).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var result = new JObject()
            {
                new JProperty("via", ReferenceUriMember),
                new JProperty("schema", Schema.ToJson().MakeNestedProp())
            };

            if (AggregationRules is not null)
                result.Add(new JProperty("$aggregation", new JArray(AggregationRules.Select(ar => ar.ToString()))));
            if (VersioningRules is not null)
                result.Add(new JProperty("$versioning", VersioningRules.ToString()));

            return new JProperty("validate", result);
        }

        /// <summary>
        /// Splits a canonical into its url and its version string.
        /// </summary>
        private static (string url, string? version) splitCanonical(string canonical)
        {
            if (canonical.EndsWith('|')) canonical = canonical[..^1];

            var position = canonical.LastIndexOf('|');

            return position == -1 ?
                (canonical, null)
                : (canonical[0..position], canonical[(position + 1)..]);
        }
    }
}
