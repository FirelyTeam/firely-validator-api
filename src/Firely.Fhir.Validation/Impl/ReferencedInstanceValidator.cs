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

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Fetches an instance by reference and starts validation against a schema. The reference is 
    /// to be found at runtime in the "reference" child of the input.
    /// </summary>
    [DataContract]
    public class ReferencedInstanceValidator : IValidatable
    {
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
        public IReadOnlyCollection<AggregationMode>? AggregationRules { get; private set; }

        /// <summary>
        /// Additional rules about versioning of the reference.
        /// </summary>
        [DataMember]
        public ReferenceVersionRules? VersioningRules { get; private set; }

        /// <summary>
        /// Create a <see cref="ReferencedInstanceValidator"/>.
        /// </summary>
        public ReferencedInstanceValidator(IAssertion schema,
            IEnumerable<AggregationMode>? aggregationRules = null, ReferenceVersionRules? versioningRules = null)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            AggregationRules = aggregationRules?.ToArray();
            VersioningRules = versioningRules;
        }

        /// <summary>
        /// Whether any <see cref="AggregationRules"/> have been specified on the constructor.
        /// </summary>
        public bool HasAggregation => AggregationRules?.Any() ?? false;

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext, ValidationState)"/>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            if (!IsSupportedReferenceType(input.InstanceType))
                return new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                    $"Expected a reference type here (reference or canonical) not a {input.InstanceType}.")
                    .AsResult(state);

            // Get the actual reference from the instance by the pre-configured name.
            // The name is usually "reference" in case we are dealing with a FHIR reference type,
            // or "$this" if the input is a canonical (which is primitive).  This may of course
            // be different for different modelling paradigms.
            var reference = input.InstanceType switch
            {
                "Reference" => input.Children("reference").FirstOrDefault()?.Value as string,
                "canonical" => input.Value as string,
                _ => throw new InvalidOperationException("Checking reference type should have been handled already.")
            };

            // It's ok for a reference to have no value (but, say, a description instead),
            // so only go out to fetch the reference if we have one.
            if (reference is not null)
            {
                // Try to fetch the reference, which will also validate the aggregation/versioning rules etc.
                var (evidence, resolution) = fetchReference(input, reference, vc, state);

                // If the reference was resolved (either internally or externally), validate it
                var referenceResolutionReport = resolution.ReferencedResource switch
                {
                    null when vc.ResolveExternalReference is null => ResultReport.SUCCESS,
                    null => new IssueAssertion(
                        Issue.UNAVAILABLE_REFERENCED_RESOURCE,
                        $"Cannot resolve reference {reference}").AsResult(state),
                    _ => validateReferencedResource(reference, vc, resolution, state)
                };

                return ResultReport.FromEvidence(evidence.Append(referenceResolutionReport).ToList());
            }
            else
                return ResultReport.SUCCESS;
        }

        private record ResolutionResult(ITypedElement? ReferencedResource, AggregationMode? ReferenceKind, ReferenceVersionRules? VersioningKind);

        /// <summary>
        /// Try to fetch the referenced resource. The resource may be present in the instance (bundled, contained)
        /// or externally. In the last case, the <see cref="ValidationContext.ExternalReferenceResolver"/> is used
        /// to fetch the resource.
        /// </summary>
        private (IReadOnlyCollection<ResultReport>, ResolutionResult) fetchReference(ITypedElement input, string reference, ValidationContext vc, ValidationState s)
        {
            ResolutionResult resolution = new(null, null, null);
            List<ResultReport> evidence = new();

            if (input is not ScopedNode instance)
                throw new InvalidOperationException($"Cannot validate because input is not of type {nameof(ScopedNode)}.");

            // First, try to resolve within this instance (in contained, Bundle.entry)
            evidence.Add(resolveLocally(instance, reference, s, out resolution));

            // Now that we have tried to fetch the reference locally, we have also determined the kind of
            // reference we are dealing with, so check it for aggregation and versioning rules.
            if (HasAggregation && !AggregationRules.Any(a => a == resolution.ReferenceKind))
            {
                var allowed = string.Join(", ", AggregationRules);
                evidence.Add(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                    $"Encountered a reference ({reference}) of kind '{resolution.ReferenceKind}', which is not one of the allowed kinds ({allowed}).")
                    .AsResult(s));
            }

            if (VersioningRules is not null && VersioningRules != ReferenceVersionRules.Either)
            {
                if (VersioningRules != resolution.VersioningKind)
                    evidence.Add(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                        $"Expected a {VersioningRules} versioned reference but found {resolution.VersioningKind}.")
                        .AsResult(s));
            }

            if (resolution.ReferenceKind == AggregationMode.Referenced)
            {
                // Bail out if we are asked to follow an *external reference* when this is disabled in the settings
                if (vc.ResolveExternalReference is null)
                    return (evidence, resolution);

                // If we are supposed to resolve the reference externally, then do so now.
                if (resolution.ReferencedResource is null)
                {
                    try
                    {
                        var externalReference = vc.ResolveExternalReference!(reference, input.Location);
                        resolution = resolution with { ReferencedResource = externalReference };
                    }
                    catch (Exception e)
                    {
                        evidence.Add(new IssueAssertion(
                            Issue.UNAVAILABLE_REFERENCED_RESOURCE,
                            $"Resolution of external reference {reference} failed. Message: {e.Message}")
                            .AsResult(s));
                    }
                }
            }

            return (evidence, resolution);
        }

        /// <summary>
        /// Try to fetch the resource within this instance (e.g. a contained or bundled resource).
        /// </summary>
        private static ResultReport resolveLocally(ScopedNode instance, string reference, ValidationState s, out ResolutionResult resolution)
        {
            resolution = new ResolutionResult(null, null, null);
            var identity = new ResourceIdentity(reference);

            var (url, version, _) = new Canonical(reference);
            resolution = resolution with { VersioningKind = version is not null ? ReferenceVersionRules.Specific : ReferenceVersionRules.Independent };

            if (identity.Form == ResourceIdentityForm.Undetermined)
            {
                if (!Uri.IsWellFormedUriString(Uri.EscapeDataString(reference), UriKind.RelativeOrAbsolute))
                {
                    return new IssueAssertion(Issue.CONTENT_UNPARSEABLE_REFERENCE,
                        $"Encountered an unparseable reference ({reference}").AsResult(s);
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

            return ResultReport.SUCCESS;
        }

        /// <summary>
        /// Validate the referenced resource against the <see cref="Schema"/>.
        /// </summary>
        private ResultReport validateReferencedResource(string reference, ValidationContext vc, ResolutionResult resolution, ValidationState state)
        {
            if (resolution.ReferencedResource is null) throw new ArgumentException("Resolution should have a non-null referenced resource by now.");

            //result += Trace($"Starting validation of referenced resource {reference} ({encounteredKind})");

            // References within the instance are dealt with within the same validator,
            // references to external entities will operate within a new instance of a validator (and hence a new tracking context).
            // In both cases, the outcome is included in the result.
            if (resolution.ReferenceKind != AggregationMode.Referenced)
                return Schema.ValidateOne(resolution.ReferencedResource, vc, state.UpdateInstanceLocation(dp => dp.AddInternalReference(resolution.ReferencedResource.Location)));
            else
            {
                //TODO: We're using state to track the external URL, but this actually would be better
                //implemented on the ScopedNode instead - add this (and combine with FullUrl?) there.
                var newState = state.NewInstanceScope();
                newState.Instance.ResourceUrl = reference;
                return Schema.ValidateOne(new ScopedNode(resolution.ReferencedResource), vc, newState);
            }
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var result = new JObject()
            {
                new JProperty("schema", Schema.ToJson().MakeNestedProp())
            };

            if (AggregationRules is not null)
                result.Add(new JProperty("$aggregation", new JArray(AggregationRules.Select(ar => ar.ToString()))));
            if (VersioningRules is not null)
                result.Add(new JProperty("$versioning", VersioningRules.ToString()));

            return new JProperty("validate", result);
        }

        /// <summary>
        /// Whether this validator recognizes the given type as a reference type.
        /// </summary>
        public static bool IsSupportedReferenceType(string typeCode) => typeCode == "Reference";    // || typeCode == "canonical"
    }
}
