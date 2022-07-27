/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
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
    /// An <see cref="ElementSchema"/> that represents a FHIR datatype or resource.
    /// </summary>
    public class FhirSchema : ElementSchema
    {
        /// <summary>
        /// A collection of information from the StructureDefintion from which
        /// this schema was generated.
        /// </summary>
        [DataMember]
        public StructureDefinitionInformation StructureDefinition { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="FhirSchema"/>
        /// </summary>
        public FhirSchema(StructureDefinitionInformation sdi, params IAssertion[] members) : this(sdi, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="FhirSchema"/>
        /// </summary>
        public FhirSchema(StructureDefinitionInformation sdi, IEnumerable<IAssertion> members) : base(sdi.Canonical, members)
        {
            StructureDefinition = sdi;
        }

        /// <summary>
        /// The kind of schema we are defining. Used to distinguish the subclasses of <see cref="FhirSchema"/>
        /// in the Json rendering.
        /// </summary>
        protected virtual string FhirSchemaKind => "element";

        /// <inheritdoc />
        protected override IEnumerable<JProperty> MetadataProps()
        {
            yield return new JProperty("schema-subtype", FhirSchemaKind);
            yield return (JProperty)StructureDefinition.ToJson();
        }

        /// <inheritdoc />
        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            // Schemas representing the root of a FHIR type cannot meaningfully be used as a GroupValidatable,
            // so for us, and our subclasses, we'll turn this into a normal IValidatable.
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
                var typeProfile = Canonical.ForCoreType(input.InstanceType);
                var fetchResult = FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, input.Location, typeProfile);
                return fetchResult.Success ? fetchResult.Schema!.Validate(input, vc, state) : fetchResult.Error!;
            }

            // FHIR has a few occasions where the schema needs to read into the instance to obtain additional schemas to
            // validate against (Resource.meta.profile, Extension.url). Fetch these from the instance and combine them into
            // a coherent set to validate against.
            var additionalCanonicals = GetAdditionalSchemas(input);
            var additionalFetches = FhirSchemaGroupAnalyzer.FetchSchemas(vc.ElementSchemaResolver, input.Location, additionalCanonicals);
            var fetchErrors = additionalFetches.Where(f => !f.Success).Select(f => f.Error!);

            var fetchedSchemas = additionalFetches.Where(f => f.Success).Select(f => f.Schema!).ToArray();
            var fetchedFhirSchemas = fetchedSchemas.OfType<FhirSchema>().ToArray();
            var fetchedNonFhirSchemas = fetchedSchemas.Where(fs => fs is not FhirSchema).ToArray();

            //if (fetchedSchemas.Length != fetchedFhirSchemas.Length)
            //    throw new InvalidOperationException($"Schemas for datatypes and resources should be of type FhirSchema, which {nonFhirSchemas()} are not.");

            var consistencyReport = FhirSchemaGroupAnalyzer.ValidateConsistency(null, null, fetchedFhirSchemas, input.Location);
            var minimalSet = FhirSchemaGroupAnalyzer.CalculateMinimalSet(fetchedFhirSchemas.Append(this));

            var validationResult = minimalSet.Select(s => s.ValidateConstraints(input, vc, state)).ToList();
            var validationResultOther = fetchedNonFhirSchemas.Select(s => s.Validate(input, vc, state)).ToList();
            return ResultReport.FromEvidence(fetchErrors.Append(consistencyReport).Concat(validationResult).Concat(validationResultOther).ToArray());

            //string nonFhirSchemas() => string.Join(',', fetchedSchemas.Where(fs => fs is not FhirSchema).Select(fs => fs.Id.ToString()));
        }

        /// <summary>
        /// Given an instance, retrieve additional schema's to validate against.
        /// </summary>
        protected virtual Canonical[] GetAdditionalSchemas(ITypedElement instance) => Array.Empty<Canonical>();

        /// <summary>
        /// Validate a FHIR schema against the constraints - which excludes the special functionality
        /// already run by the <see cref="FhirSchema.Validate(ITypedElement, ValidationContext, ValidationState)"/> implementation.
        /// </summary>
        /// <remarks>
        /// This is the one method all subclasses should override, instead of <see cref="IValidatable.Validate(ITypedElement, ValidationContext, ValidationState)"/>
        /// </remarks>
        protected virtual ResultReport ValidateConstraints(ITypedElement input, ValidationContext vc, ValidationState vs) => base.Validate(input, vc, vs);

        /// <summary>
        /// Determines whether this FhirSchema includes all constraints from the given schema, and
        /// so is a superset of that schema.
        /// </summary>
        public bool IsSupersetOf(Canonical other) => StructureDefinition.BaseCanonicals?.Contains(other) == true;

        /// <summary>
        /// The canonical from the included <see cref="StructureDefinitionInformation"/>.
        /// </summary>
        public Canonical Url => Id;
    }
}
