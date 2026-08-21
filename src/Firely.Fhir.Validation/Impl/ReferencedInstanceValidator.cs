/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The checks a <see cref="ReferencedInstanceValidator"/> performs on the target of a reference.
    /// The checks on the reference value itself (parseability, aggregation and versioning rules)
    /// always run.
    /// </summary>
    [Flags]
#if NET8_0_OR_GREATER
    [Experimental(diagnosticId: "ExperimentalApi")]
#else
    [Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public enum ReferenceChecks
    {
        /// <summary>
        /// Check nothing about the target. The reference is not resolved, unless aggregation
        /// rules require determining the kind of reference; the checks on the reference value
        /// itself still run.
        /// </summary>
        None = 0,

        /// <summary>
        /// Check that the reference can be resolved.
        /// </summary>
        Exists = 1,

        /// <summary>
        /// Check that the reference resolves to a resource of one of the allowed target types
        /// (see <see cref="ReferencedInstanceValidator.TargetCase.Type"/>).
        /// </summary>
        TargetType = 2,

        /// <summary>
        /// Validate the resolved target against the profiles applicable to its type. Note that
        /// selecting those profiles requires the target's type to match one of the target cases,
        /// so a target matching no case is reported even when <see cref="TargetType"/> is not set.
        /// </summary>
        TargetProfile = 4,

        /// <summary>
        /// All checks, the validator's standard behavior.
        /// </summary>
        All = Exists | TargetType | TargetProfile
    }

    /// <summary>
    /// Fetches an instance by reference and starts validation against a schema. The reference is
    /// to be found at runtime in the "reference" child of the input.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [Experimental(diagnosticId: "ExperimentalApi")]
#else
    [Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class ReferencedInstanceValidator : IValidatable
    {
        /// <summary>
        /// The schema to validate the target of a reference against, for targets of a given type:
        /// the explicit representation of the target types allowed by a reference.
        /// </summary>
        [DataContract]
        public class TargetCase
        {
            /// <summary>
            /// The type of target resource this case applies to. This may be an abstract base type
            /// (e.g. <c>Resource</c> for references without target profiles), which matches all its
            /// derived types.
            /// </summary>
            [DataMember]
            public string Type { get; private set; }

            /// <summary>
            /// The schema to validate targets of this <see cref="Type"/> against, normally (a choice
            /// of) the target profile(s) declared for that type.
            /// </summary>
            [DataMember]
            public IAssertion Schema { get; private set; }

            /// <summary>
            /// Constructs a case for targets of the given type.
            /// </summary>
            public TargetCase(string type, IAssertion schema)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            }
        }

        /// <summary>
        /// When the referenced resource was found, it will be validated against
        /// this schema. <c>null</c> when this validator dispatches on the target's type using
        /// <see cref="TargetCases"/> instead.
        /// </summary>
        [DataMember]
        public IAssertion? Schema { get; private set; }

        /// <summary>
        /// The allowed target types and the schema to validate each type's targets against.
        /// <c>null</c> when this validator validates every target against <see cref="Schema"/>.
        /// </summary>
        [DataMember]
        public IReadOnlyList<TargetCase>? TargetCases { get; private set; }

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
        /// The checks this validator performs on the target of the reference. Defaults to
        /// <see cref="ReferenceChecks.All"/>, the validator's standard behavior.
        /// </summary>
        [DataMember]
        public ReferenceChecks Checks { get; private set; } = ReferenceChecks.All;

        /// <summary>
        /// Create a <see cref="ReferencedInstanceValidator"/> that validates every target against
        /// a single schema, without checking the target's type.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public ReferencedInstanceValidator(IAssertion schema,
            IEnumerable<AggregationMode>? aggregationRules = null, ReferenceVersionRules? versioningRules = null)
#pragma warning restore RS0026
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            AggregationRules = aggregationRules?.ToArray();
            VersioningRules = versioningRules;
        }

        /// <summary>
        /// Create a <see cref="ReferencedInstanceValidator"/> that dispatches on the type of the
        /// target: the target must be of one of the case types, and is validated against the schema
        /// of the matching case.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public ReferencedInstanceValidator(IEnumerable<TargetCase> targetCases,
            IEnumerable<AggregationMode>? aggregationRules = null, ReferenceVersionRules? versioningRules = null,
            ReferenceChecks checks = ReferenceChecks.All)
#pragma warning restore RS0026
        {
            var cases = targetCases?.ToArray() ?? throw new ArgumentNullException(nameof(targetCases));
            if (cases.Length == 0)
                throw new ArgumentException("At least one target case is required - a validator without cases could never accept a target.", nameof(targetCases));
            if (cases.Select(c => c.Type).Distinct().Count() != cases.Length)
                throw new ArgumentException("Target case types must be unique - dispatch always selects the first case for a type, so later duplicates would be unreachable.", nameof(targetCases));

            TargetCases = cases;
            AggregationRules = aggregationRules?.ToArray();
            VersioningRules = versioningRules;
            Checks = checks;

            // The common "any resource" reference (no target profiles) is compiled to a single
            // "Resource" case - matching it needs no type dispatch at all, so precompute that.
            _catchAllCase = cases is [{ Type: "Resource" } single] ? single : null;
        }

        private readonly TargetCase? _catchAllCase;

        /// <summary>
        /// Whether any <see cref="AggregationRules"/> have been specified on the constructor.
        /// </summary>
        public bool HasAggregation => AggregationRules?.Any() ?? false;

        /// <summary>
        /// Whether the current <see cref="Checks"/> require the target of the reference to be resolved.
        /// </summary>
        private bool needsTarget => Checks != ReferenceChecks.None;

        /// <inheritdoc cref="IValidatable.Validate(PocoNode, ValidationSettings, ValidationState)"/>
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            if (vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationSettings)} does not contain an ElementSchemaResolver.");
            
            if (!IsSupportedReferenceType(input.Poco.TypeName))
                return new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                    $"Expected a reference type here (reference or canonical) not a {input.Poco.TypeName}.")
                    .AsResult(state, input, nameof(ReferencedInstanceValidator), this);

            // Get the actual reference from the instance by the pre-configured name.
            // The name is usually "reference" in case we are dealing with a FHIR reference type,
            // or "$this" if the input is a canonical (which is primitive).  This may of course
            // be different for different modelling paradigms.
            var reference = input.Poco switch
            {
                // when we're sure there's no overflow we can use poco, otherwise there might've been 2 reference elements
                ResourceReference resourceRef when !input.Poco.HasOverflow => resourceRef.Reference,
                CodeableReference codeableRef when !input.Poco.HasOverflow => codeableRef.Reference?.Reference,
                ResourceReference => input.NavigateTo("reference").FirstOrDefault()?.GetValue() as string,
                CodeableReference => input.NavigateTo("reference.reference").FirstOrDefault()?.GetValue() as string,
                Hl7.Fhir.Model.Canonical canonical => canonical.Value,
                var unknown => throw new NotSupportedException($"Encountered unsupported reference type {unknown.TypeName}.")
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
                    null when !Checks.HasFlag(ReferenceChecks.Exists) => ResultReport.SUCCESS,
                    null when vc.ResolveExternalReference is null => ResultReport.SUCCESS,
                    null => new IssueAssertion(
                        Issue.UNAVAILABLE_REFERENCED_RESOURCE,
                        $"Cannot resolve reference {reference}").AsResult(state, input, nameof(ReferencedInstanceValidator), this),
                    _ => validateReferencedResource(reference, vc, resolution, state)
                };

                return ResultReport.Combine(evidence.Append(referenceResolutionReport).ToList());
            }
            else
                return ResultReport.SUCCESS;
        }

        private record ResolutionResult(PocoNode? ReferencedResource, AggregationMode? ReferenceKind, ReferenceVersionRules? VersioningKind);

        /// <summary>
        /// Try to fetch the referenced resource. The resource may be present in the instance (bundled, contained)
        /// or externally. In the last case, the <see cref="ExternalReferenceResolver"/> is used
        /// to fetch the resource.
        /// </summary>
        private (IReadOnlyCollection<ResultReport>, ResolutionResult) fetchReference(PocoNode input, string reference, ValidationSettings vc, ValidationState s)
        {
            // The resolved resource is only needed for the target checks; the kind of reference
            // (contained/bundled/referenced), which requires resolution too, only for the
            // aggregation rules. Without either, resolution can be skipped altogether.
            var needsResolution = needsTarget || HasAggregation;

            List<ResultReport> evidence =
            [
                // First, try to resolve within this instance (in contained, Bundle.entry)
                resolveLocally(input, reference, needsResolution, s, out var resolution)
            ];

            // Now that we have tried to fetch the reference locally, we have also determined the kind of
            // reference we are dealing with, so check it for aggregation and versioning rules.
            if (HasAggregation && AggregationRules?.Any(a => a == resolution.ReferenceKind) == false)
            {
                var allowed = string.Join(", ", AggregationRules);
                evidence.Add(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                    $"Encountered a reference ({reference}) of kind '{resolution.ReferenceKind}', which is not one of the allowed kinds ({allowed}).")
                    .AsResult(s, input, nameof(ReferencedInstanceValidator), this));
            }

            if (VersioningRules is not null && VersioningRules != ReferenceVersionRules.Either)
            {
                if (VersioningRules != resolution.VersioningKind)
                    evidence.Add(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND,
                        $"Expected a {VersioningRules} versioned reference but found {resolution.VersioningKind}.")
                        .AsResult(s, input, nameof(ReferencedInstanceValidator), this));
            }

            if (resolution.ReferenceKind == AggregationMode.Referenced)
            {
                // Bail out if we are asked to follow an *external reference* when this is disabled in
                // the settings, or when none of the enabled checks need the target.
                if (vc.ResolveExternalReference is null || !needsTarget)
                    return (evidence, resolution);

                // If we are supposed to resolve the reference externally, then do so now.
                if (resolution.ReferencedResource is null)
                {
                    try
                    {
                        var externalReference = vc.ResolveExternalReference!(reference, input.GetLocation());
                        resolution = resolution with { ReferencedResource = externalReference };
                    }
                    catch (Exception e)
                    {
                        evidence.Add(new IssueAssertion(
                            Issue.UNAVAILABLE_REFERENCED_RESOURCE,
                            $"Resolution of external reference {reference} failed. Message: {e.Message}")
                            .AsResult(s, input, nameof(ReferencedInstanceValidator), this));
                    }
                }
            }

            return (evidence, resolution);
        }

        /// <summary>
        /// Try to fetch the resource within this instance (e.g. a contained or bundled resource).
        /// The reference value itself is checked regardless, but the (potentially expensive) lookup
        /// of the target is only done when <paramref name="resolve"/> is true.
        /// </summary>
        private ResultReport resolveLocally(PocoNode instance, string reference, bool resolve, ValidationState s, out ResolutionResult resolution)
        {
            resolution = new ResolutionResult(null, null, null);
            var identity = new ResourceIdentity(reference);

            var (_, version, _) = new Canonical(reference);
            resolution = resolution with { VersioningKind = version is not null ? ReferenceVersionRules.Specific : ReferenceVersionRules.Independent };

            if (identity.Form == ResourceIdentityForm.Undetermined)
            {
                if (!Uri.IsWellFormedUriString(Uri.EscapeDataString(reference), UriKind.RelativeOrAbsolute))
                {
                    return new IssueAssertion(Issue.CONTENT_UNPARSEABLE_REFERENCE,
                        $"Encountered an unparseable reference ({reference}").AsResult(s, instance.ToPocoNode(), nameof(ReferencedInstanceValidator), this);
                }
            }

            PocoNode? referencedResource;

            try
            {
                referencedResource = resolve ? instance.Resolve(reference) : null;
            }
            catch (Exception e)
            {
                return new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                    $"Encountered an issue during reference resolution. Message: {e.Message}").AsResult(s, instance.ToPocoNode(), nameof(ReferencedInstanceValidator), this);
            }


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
        /// Validate the referenced resource against the <see cref="Schema"/> or the matching
        /// <see cref="TargetCases"/> case.
        /// </summary>
        private ResultReport validateReferencedResource(string reference, ValidationSettings vc, ResolutionResult resolution, ValidationState state)
        {
            if (resolution.ReferencedResource is null) throw new ArgumentException("Resolution should have a non-null referenced resource by now.");

            //result += Trace($"Starting validation of referenced resource {reference} ({encounteredKind})");

            // References within the instance are dealt with within the same validator,
            // references to external entities will operate within a new instance of a validator (and hence a new tracking context).
            // In both cases, the outcome is included in the result.
            if (resolution.ReferenceKind != AggregationMode.Referenced)
                return validateTarget(reference, resolution.ReferencedResource.ToPocoNode(), vc, state);
            else
            {
                //TODO: We're using state to track the external URL, but this actually would be better
                //implemented on the ScopedNode instead - add this (and combine with FullUrl?) there.
                var newState = state.NewInstanceScope();
                newState.Instance.ResourceUrl = reference;
                return validateTarget(reference, resolution.ReferencedResource.ToPocoNode(), vc, newState);
            }
        }

        /// <summary>
        /// Runs the target checks enabled in <see cref="Checks"/> on the resolved target: the type
        /// dispatch over <see cref="TargetCases"/> and/or the validation against the (matching) schema.
        /// </summary>
        private ResultReport validateTarget(string reference, PocoNode target, ValidationSettings vc, ValidationState state)
        {
            // The legacy single-schema form does no type checking of its own, and can only be
            // constructed with Checks == All, so the target is always validated against the schema.
            if (TargetCases is null)
                return Schema!.ValidateOne(target, vc, state);

            if (!Checks.HasFlag(ReferenceChecks.TargetType) && !Checks.HasFlag(ReferenceChecks.TargetProfile))
                return ResultReport.SUCCESS;

            var typeName = target.Poco.TypeName;

            if (findTargetCase(typeName, target, vc) is not { } matchingCase)
            {
                var allowed = string.Join(", ", TargetCases.Select(c => $"'{c.Type}'"));
                return new IssueAssertion(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE,
                        $"Referenced resource '{reference}' is of type '{typeName}', which is not one of the allowed target types ({allowed}).")
                    .AsResult(state, target, nameof(ReferencedInstanceValidator), this);
            }

            return Checks.HasFlag(ReferenceChecks.TargetProfile)
                ? matchingCase.Schema.ValidateOne(target, vc, state)
                : ResultReport.SUCCESS;
        }

        /// <summary>
        /// Finds the case that applies to a target of the given type. Case types may be abstract
        /// base types (e.g. <c>Resource</c>), which match all their derived types.
        /// </summary>
        private TargetCase? findTargetCase(string typeName, PocoNode target, ValidationSettings vc)
        {
            // The single "Resource" case matches every resource target - no dispatch needed. The
            // POCO type test keeps this shortcut honest for the pathological case of an external
            // resolver handing back a non-resource node: that falls through to the normal matching.
            if (_catchAllCase is not null && target.Poco is Resource) return _catchAllCase;

            if (TargetCases!.FirstOrDefault(c => c.Type == typeName) is { } exact) return exact;

            var inspector = vc.GetModelInspector(target);
            return TargetCases!.FirstOrDefault(c => inspector.IsInstanceTypeFor(c.Type, typeName));
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var result = new JObject();

            if (Schema is not null)
                result.Add(new JProperty("schema", Schema.ToJson().MakeNestedProp()));
            if (TargetCases is not null)
                result.Add(new JProperty("targets", new JObject(
                    TargetCases.Select(c => new JProperty(c.Type, c.Schema.ToJson().MakeNestedProp())))));

            if (AggregationRules is not null)
                result.Add(new JProperty("$aggregation", new JArray(AggregationRules.Select(ar => ar.ToString()))));
            if (VersioningRules is not null)
                result.Add(new JProperty("$versioning", VersioningRules.ToString()));
            if (Checks != ReferenceChecks.All)
                result.Add(new JProperty("checks", Checks.ToString()));

            return new JProperty("validate", result);
        }

        /// <summary>
        /// Whether this validator supports validating a given reference type.
        /// </summary>
        internal static bool IsSupportedReferenceType(string typeCode) =>
            IsReferenceType(typeCode) && typeCode is not "canonical";

        /// <summary>
        /// Whether a type is a reference type.
        /// </summary>
        /// <param name="typeCode"></param>
        /// <returns></returns>
        internal static bool IsReferenceType(string typeCode) => typeCode is "Reference" or "CodeableReference" or "canonical";
    }
}
