/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
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
    /// Asserts the validity of an instance against the expected type, or in the case of a reference, against the
    /// stated target profile.
    /// </summary>
    [DataContract]
    public class ReferenceAssertion : IGroupValidatable
    {
        private const string RESOURCE_URI = "http://hl7.org/fhir/StructureDefinition/Resource";
        private const string REFERENCE_URI = "http://hl7.org/fhir/StructureDefinition/Reference";

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public Uri ReferencedUri { get; private set; }

        [DataMember(Order = 1)]
        public IEnumerable<AggregationMode>? Aggregations { get; private set; }
#else
        [DataMember]
        public Uri ReferencedUri { get; private set; }

        [DataMember]
        public IEnumerable<AggregationMode>? Aggregations { get; private set; }
#endif


        public ReferenceAssertion(Uri referencedUri, IEnumerable<AggregationMode>? aggregations = null)
        {
            ReferencedUri = referencedUri;
            Aggregations = aggregations;
        }

        public ReferenceAssertion(string referencedUri, IEnumerable<AggregationMode>? aggregations = null)
        {
            ReferencedUri = new Uri(referencedUri, UriKind.RelativeOrAbsolute);
            Aggregations = aggregations;
        }

        private bool HasAggregation => Aggregations?.Any() ?? false;

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            if (vc.ElementSchemaResolver is null)
            {
                return Assertions.EMPTY + ResultAssertion.CreateFailure(new IssueAssertion(
                          Issue.PROCESSING_CATASTROPHIC_FAILURE, null,
                          $"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver."));
            }

            return (ReferencedUri.ToString()) switch
            {
                RESOURCE_URI => await input.Select(i => ValidationExtensions.Validate(getCanonical(i), i, vc)).AggregateAsync(),
                REFERENCE_URI => await input.Select(i => validateReference(i, vc)).AggregateAsync(),
                _ => await ValidationExtensions.Validate(ReferencedUri, input, vc)
            };

        }

        private async Task<Assertions> validateReference(ITypedElement input, ValidationContext vc)
        {
            var result = Assertions.EMPTY;

            if (input is not ScopedNode instance)
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(
                        Issue.PROCESSING_CATASTROPHIC_FAILURE, input.Location,
                        $"Cannot validate because input is not of type {nameof(ScopedNode)}."));
                return result;
            }

            var reference = instance.ParseResourceReference().Reference;
            if (reference is null)
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(
                    Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance.Location,
                    $"Could not find reference in instance"));
                return result;
            }

            result += resolveReference(instance, reference, out (ITypedElement? referencedResource, AggregationMode? encounteredKind) referenceInstance);
            var referencedResource = referenceInstance.referencedResource;

            result += validateAggregation(referenceInstance.encounteredKind, input.Location, reference);

            // Bail out if we are asked to follow an *external reference* when this is disabled in the settings
            if (vc.ResolveExternalReferences == false && referenceInstance.encounteredKind == AggregationMode.Referenced)
                return result;

            // If we failed to find a referenced resource within the current instance, try to resolve it using an external method
            //TODO
            if (referencedResource is null && referenceInstance.encounteredKind == AggregationMode.Referenced)
            {
                try
                {
                    referencedResource = vc.ExternalReferenceResolutionNeeded(reference, instance.Location, result);
                }
                catch (Exception e)
                {
                    result += ResultAssertion.CreateFailure(new IssueAssertion(
                        Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance.Location,
                        $"Resolution of external reference {reference} failed. Message: {e.Message}"));
                }
            }

            // If the reference was resolved (either internally or externally), validate it
            result += await validateReferencedResource(vc, instance, reference, referenceInstance, referencedResource);

            return result;
        }

        private async Task<Assertions> validateReferencedResource(ValidationContext vc, ScopedNode instance, string reference, (ITypedElement? referencedResource, AggregationMode? encounteredKind) referenceInstance, ITypedElement? referencedResource)
        {
            var result = Assertions.EMPTY;

            if (referencedResource is not null)
            {
                //result += Trace($"Starting validation of referenced resource {reference} ({encounteredKind})");

                // References within the instance are dealt with within the same validator,
                // references to external entities will operate within a new instance of a validator (and hence a new tracking context).
                // In both cases, the outcome is included in the result.
                //OperationOutcome childResult;

                // TODO: BRIAN: Check that this TargetProfile.FirstOrDefault() is actually right, or should
                //              we be permitting more than one target profile here.
                if (referenceInstance.encounteredKind != AggregationMode.Referenced)
                {
                    result += await ValidationExtensions.Validate(getCanonical(referencedResource), referencedResource, vc);
                }
                else
                {
                    // TODO

                    //var newValidator = validator.NewInstance();
                    //childResult = newValidator.ValidateReferences(referencedResource, typeRef.TargetProfile);
                }
            }
            else
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(
                    Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance.Location,
                    $"Cannot resolve reference {reference}"));
            }

            return result;
        }

        private Assertions validateAggregation(AggregationMode? encounteredKind, string location, string reference)
        {
            var result = Assertions.EMPTY;

            // Validate the kind of aggregation.
            // If no aggregation is given, all kinds of aggregation are allowed, otherwise only allow
            // those aggregation types that are given in the Aggregation element
            if (HasAggregation && !Aggregations.Any(a => a == encounteredKind))
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_REFERENCE_OF_INVALID_KIND, location, $"Encountered a reference ({reference}) of kind '{encounteredKind}' which is not allowed"));
            }

            return result;
        }

        private static Assertions resolveReference(ScopedNode instance, string reference, out (ITypedElement?, AggregationMode?) referenceInstance)
        {
            var result = Assertions.EMPTY;
            var identity = new ResourceIdentity(reference);

            if (identity.Form == ResourceIdentityForm.Undetermined)
            {
                if (!Uri.IsWellFormedUriString(Uri.EscapeDataString(reference), UriKind.RelativeOrAbsolute))
                {
                    result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_UNPARSEABLE_REFERENCE, "TODO", $"Encountered an unparseable reference ({reference}"));
                    referenceInstance = (null, null);
                    return result;
                }
            }

            var referencedResource = instance.Resolve(reference);
            AggregationMode? aggregationMode;
            if (identity.Form == ResourceIdentityForm.Local)
            {
                aggregationMode = AggregationMode.Contained;
                if (referencedResource == null)
                    result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_CONTAINED_REFERENCE_NOT_RESOLVABLE, "TODO", $"Contained reference ({reference}) is not resolvable"));
            }
            else
            {
                aggregationMode = referencedResource != null ? AggregationMode.Bundled : AggregationMode.Referenced;
            }

            referenceInstance = (referencedResource, aggregationMode);
            return result;
        }


        private static Uri getCanonical(ITypedElement input)
            => new Uri($"{ResourceIdentity.CORE_BASE_URL}{input.InstanceType}");

        public JToken ToJson() => new JProperty("$ref", ReferencedUri?.ToString() ??
            throw Error.InvalidOperation("Cannot convert to Json: reference refers to a schema without an identifier"));
    }
}
