/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR datatype or resource.
    /// </summary>
    public abstract class FhirSchema : ElementSchema
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
        protected abstract string FhirSchemaKind { get; }

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
                var (schema, error) = SchemaReferenceValidator.FetchSchema(typeProfile, vc.ElementSchemaResolver, input.Location);
                return schema is not null ? schema.Validate(input, vc, state) : error!;
            }

            // Otherwise, we're just a normal schema, call base validation.
            return base.Validate(input, vc, state);
        }

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
