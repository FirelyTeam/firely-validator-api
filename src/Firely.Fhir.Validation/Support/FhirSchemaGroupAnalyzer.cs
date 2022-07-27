/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// This class has utility functions to process type information and profiles to validate against, and 
    /// analyzes their consistency. 
    /// </summary>
    /// <remarks>This class is highly dependent on the presence of the FHIR-specific <see cref="StructureDefinitionInformation"/>
    /// in ElementSchema, so it will only work for <see cref="FhirSchema"/> types that are derived from FHIR StructureDefinitions.</remarks>
    public static class FhirSchemaGroupAnalyzer
    {
        /// <summary>
        /// The result of resolving a schema: either a schema, or a <see cref="ResultReport"/> detailing the failure.
        /// </summary>
        public record SchemaFetchResult(ElementSchema? Schema, ResultReport? Error)
        {
            /// <summary>
            /// Indicates whether fetching the schema by canonical has succeeded.
            /// </summary>
            public bool Success => Schema is not null;
        }

        /// <summary>
        /// Tries to resolve a set of canonicals, reporting on the outcome. 
        /// </summary>
        public static IReadOnlyCollection<SchemaFetchResult> FetchSchemas(IElementSchemaResolver resolver, string location, params Canonical[] canonicals) =>
            canonicals.Select(c => FetchSchema(resolver, location, c)).ToArray();

        /// <summary>
        /// Tries to resolve a canonical, reporting on the outcome. 
        /// </summary>
        public static SchemaFetchResult FetchSchema(IElementSchemaResolver resolver, string location, Canonical canonical)
        {
            ResultReport makeUnresolvableError(string message) => new(ValidationResult.Undecided, new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE, location, message));

            var (coreSchema, version, anchor) = canonical;

            if (coreSchema is null)
                return new(null, makeUnresolvableError($"Resolving to local anchors is unsupported: '{canonical}'."));

            // Resolve the uri - without the anchor part.
            if (resolver.GetSchema(new Canonical(coreSchema, version, null)) is not { } schema)
                return new(null, makeUnresolvableError($"Unable to resolve reference to profile '{canonical}'."));

            // If there is a subschema set, try to locate it.
            if (anchor is not null)
            {
                if (schema.FindFirstByAnchor(anchor) is { } subschema)
                    return new(subschema, null);
                else
                    return new(null, makeUnresolvableError($"Unable to locate anchor {anchor} within profile '{canonical}'."));
            }
            else
                return new(schema, null);
        }


        /// <summary>
        /// Validate and report on the consistency of supplied type information and profiles.
        /// </summary>
        /// <remarks>All parmaters are nullable to cater for both the static (compile time) and runtime use.</remarks>
        /// <param name="actualType">The actual type of the instance, as a full canonical.</param>
        /// <param name="declaredType">The declared type (from TypeRef.code) in the StructureDefinition, as a full canonical.</param>
        /// <param name="stated">A set of schema's to validate against, as for example declared by
        /// TypeRef.target, Meta.profile or Extension.url</param>
        /// <param name="location">The location used for error reporting.</param>        
        public static ResultReport ValidateConsistency(FhirSchema? actualType, Canonical? declaredType, FhirSchema[]? stated, string location)
        {
            // If we have an instance type, it should be compatible with the declared type on the definition
            if (actualType is not null && declaredType is not null)
            {
                if (!isAssignable(actualType, declaredType))
                    return new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, location,
                        $"The declared type of the element ({declaredType}) is incompatible with that of the instance ({actualType.Url})").AsResult();
            }

            static bool isAssignable(FhirSchema schema, Canonical @to) => schema.Url == to || schema.IsSupersetOf(to);

            var issues = new List<ResultReport>();

            // All stated profiles should be compatible with the actual and declared type for the element
            if (stated?.Any() == true)
            {
                foreach (var statedSchema in stated)
                {
                    if (declaredType is not null && !isAssignable(statedSchema, declaredType))
                        issues.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, location,
                            $"The declared type of the instance ({declaredType}) is incompatible with that of the stated profile ({statedSchema.Url}), " +
                            $"an instance cannot be valid against both.").AsResult());

                    if (actualType is not null && !isAssignable(statedSchema, actualType.Url))
                        issues.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, location,
                            $"The actual type of the instance ({actualType.Url}) is incompatible with that of the stated profile ({statedSchema.Url}), " +
                            $"an instance cannot be valid against both.").AsResult());
                }

            }

            return ResultReport.FromEvidence(issues);
        }

        /// <summary>
        /// Calculate te minimal set of schema's that represent all constraints in the total set. 
        /// E.g. When both a more specialized schema and its base are in the total set, return just the more specialized schema.        
        /// </summary>
        /// <remarks>This function does not enforce consistency: if two profiles are not in each others lineage, they are both returned.</remarks>
        public static FhirSchema[] CalculateMinimalSet(IEnumerable<FhirSchema> schemas)
        {
            var minimal = new List<FhirSchema>(schemas);

            foreach (var schema in schemas)
            {
                // Remove redundant bases (which are subsets of more specialized profiles),
                // since the snapshots will contain their constraints anyway.
                minimal.RemoveAll(m => schema.IsSupersetOf(m.Url));
            }

            return minimal.ToArray();
        }
    }
}
