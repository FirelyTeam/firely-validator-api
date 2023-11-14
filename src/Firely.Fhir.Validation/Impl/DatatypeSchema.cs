/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR datatype (except Extension).
    /// </summary>    
    [DataContract]
    public class DatatypeSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public DatatypeSchema(StructureDefinitionInformation structureDefinition, params IAssertion[] members) : base(structureDefinition, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public DatatypeSchema(StructureDefinitionInformation structureDefinition, IEnumerable<IAssertion> members) : base(structureDefinition, members)
        {
            // nothing
        }

        /// <inheritdoc />
        public override ResultReport Validate(IEnumerable<IScopedNode> input, ValidationContext vc, ValidationState state)
        {
            // Schemas representing the root of a FHIR datatype cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var results = input.Select((i, index) => Validate(i, vc, state.UpdateInstanceLocation(d => d.ToIndex(index))));
            return ResultReport.FromEvidence(results.ToList());
        }

        /// <inheritdoc />
        public override ResultReport Validate(IScopedNode input, ValidationContext vc, ValidationState state)
        {
            // FHIR specific rule about dealing with abstract datatypes (not profiles!): if this schema is an abstract datatype,
            // we need to run validation against the schema for the actual type, not the abstract type.
            if (StructureDefinition.IsAbstract && StructureDefinition.Derivation != StructureDefinitionInformation.TypeDerivationRule.Constraint)
            {
                if (vc.ElementSchemaResolver is null)
                    throw new ArgumentException($"Cannot validate the resource because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

                var typeProfile = Canonical.ForCoreType(input.InstanceType);
                var fetchResult = FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, state, typeProfile);
                return fetchResult.Success ? fetchResult.Schema!.Validate(input, vc, state) : fetchResult.Error!;
            }
            else
                return base.Validate(input, vc, state);

        }

        /// <inheritdoc/>
        protected override string FhirSchemaKind => "datatype";
    }
}
