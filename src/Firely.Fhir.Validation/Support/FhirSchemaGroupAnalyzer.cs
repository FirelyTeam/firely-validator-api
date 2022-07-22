/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// This class takes a set of related schema's (i.e. that are all used to validate a single instance) and 
    /// analyzes their consistency. In practice, this is the collection of declared (in the StructureDefinition),
    /// actual (coming from the instance type) and stated (in Extension.url and Meta.profile) profiles. 
    /// </summary>
    /// <remarks>This class is highly dependent on the presence of the FHIR-specific <see cref="StructureDefinitionInformation"/>
    /// in ElementSchema, so it will only work for schemas that are derived from FHIR StructureDefinitions.</remarks>
    internal class FhirSchemaGroupAnalyzer
    {
        private readonly FhirSchema _instanceSchema;
        private readonly FhirSchema _declared;
        private readonly FhirSchema[] _stated;
        private readonly string _location;

        /// <summary>
        /// Initializes a set of relevant schema's to optimize and validate for consistency.
        /// </summary>
        /// <param name="instanceSchema">The type of the instance, as a FHIR type.</param>
        /// <param name="declared">The schema that represents the element's type, coming from the ElementDefinition in the StructureDefinition.</param>
        /// <param name="stated">The schema('s) declared by Meta.profile or Extension.url</param>
        /// <param name="location">The location for error reporting.</param>
        public FhirSchemaGroupAnalyzer(FhirSchema instanceSchema, FhirSchema declared, FhirSchema[] stated, string location)
        {
            _instanceSchema = instanceSchema;
            _declared = declared;
            _stated = stated;
            _location = location;
        }

        private static StructureDefinitionInformation getBaseTypeAssertionFromSchema(FhirSchema s) => s.StructureDefinition;

        /// <summary>
        /// Validate and report on the consistency of the supplied schema's.
        /// </summary>
        public ResultReport ValidateConsistency()
        {
            var issues = new List<ResultReport>();

            // If we have an instance type, it should be compatible with the declared type on the definition and the stated profiles
            if (_instanceSchema is not null)
            {
                var fhirTypeCanonical = ResourceIdentity.Core(_instanceSchema.Id.ToString()).ToString();

                if (_declared is not null)
                {
                    if (_declared.IsSupersetOf(fhirTypeCanonical))
                        issues.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, _location, $"The declared profile of the element ({_declared.Id}) is incompatible with that of the instance ('{fhirTypeCanonical}'.)").AsResult());
                }

                foreach (var statedType in _stated)
                {
                    if (!statedType.IsSupersetOf(fhirTypeCanonical))
                        issues.Add(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, _location, $"The profile for the instance '{fhirTypeCanonical}' is incompatible with the stated profile '{statedType.Id}'.").AsResult());
                }
            }

            // All stated profiles should be profiling the same core type
            if (_stated.Any())
            {
                var baseTypes = _stated
                    .Select(s => getBaseTypeAssertionFromSchema(s)?.DataType)
                    .Where(dt => dt is not null)
                    .Distinct()
                    .ToList();

                if (baseTypes.Count > 1)
                {
                    var combinedNames = string.Join(" and ", baseTypes);
                    issues.Add(new IssueAssertion(Issue.CONTENT_MISMATCHING_PROFILES, _location, $"The stated profiles are constraints on multiple different core types ({combinedNames}), which can never be satisfied.").AsResult());
                }
                else if (baseTypes.Count == 1)
                {
                    // The stated profiles should be compatible with the declared type of the element
                    if (_declared is not null)
                    {
                        var baseTypeCanonical = ResourceIdentity.Core(baseTypes.Single()).ToString();
                        if (!_declared.IsSupersetOf(baseTypeCanonical))
                            issues.Add(new IssueAssertion(Issue.CONTENT_MISMATCHING_PROFILES, _location, $"The stated profiles are all constraints on '{baseTypes.Single()}', which is incompatible with the declared profile '{_declared}' of the element.").AsResult());
                    }
                }
            }

            return ResultReport.FromEvidence(issues);
        }

        /// <summary>
        /// Calculate te minimal set of schema's that represent all constraints in the total set. E.g. When both a more specialized schema and its
        /// base are in the total set, return just the more specialized schema.</summary>
        public ElementSchema[] CalculateMinimalSet()
        {
            // Provided validation was done, IF there are stated profiles, they are correct constraints on the instance, and compatible with the declared type
            // so we can just return that list (we might even remove the ones that are constraints on constraints)
            if (_stated.Any())
            {
                // Remove redundant bases, since the snapshots will contain their constraints anyway.
                var result = _stated.ToList(); // clone the existing list
                var bases = _stated.SelectMany(sp => getBaseTypeAssertionFromSchema(sp)?.BaseCanonicals ?? Enumerable.Empty<Canonical>()).Distinct().ToList();
                bases.AddRange(_stated
                    .Select(s => getBaseTypeAssertionFromSchema(s))
                    .Where(bt => bt is not null && bt.Derivation == StructureDefinitionInformation.TypeDerivationRule.Constraint)
                    .Select(bt => Canonical.ForCoreType(bt!.DataType))
                    .Distinct());
                result.RemoveAll(r => bases.Any(b => r.Id.ToString() == b));
                return result.ToArray();
            }

            // If there are no stated profiles, then:
            //  * If the declared type is a profile, it is more specific than the instance
            //  * If the declared type is a concrete core type, it is as specific as the instance
            // In both cases return the declared type.
            else if (_declared is not null && isSpecific())
            {
                return new[] { _declared };
            }
            // Else, all we have left is the instance type
            // If there is no known instance type, we have no profile to validate against
            else if (_instanceSchema is not null)
                return new[] { _instanceSchema };
            else
                return Array.Empty<ElementSchema>();

            bool isSpecific()
            {
                var info = getBaseTypeAssertionFromSchema(_declared);
                if (info is null) return false;

                return info!.Derivation == StructureDefinitionInformation.TypeDerivationRule.Constraint ||
                      (ResourceIdentity.Core(info.DataType).ToString() == _declared.Id.ToString() && info.IsAbstract == false);
            }
        }
    }
}
