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

        /// <inheritdoc/>
        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            state = state.UpdateLocation(sp => sp.InvokeSchema(this));
            return base.Validate(input, vc, state);
        }

        /// <inheritdoc/>
        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            state = state.UpdateLocation(sp => sp.InvokeSchema(this));
            return base.Validate(input, groupLocation, vc, state);
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
