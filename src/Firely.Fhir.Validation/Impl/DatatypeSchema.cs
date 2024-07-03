/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete
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
        internal override ResultReport ValidateInternal(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state)
        {
            // Schemas representing the root of a FHIR datatype cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var results = input.Select((i, index) => ValidateInternal(i, vc, state.UpdateInstanceLocation(d => d.ToIndex(index))));
            return ResultReport.Combine(results.ToList());
        }

        /// <inheritdoc />
        internal override ResultReport ValidateInternal(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            // FHIR specific rule about dealing with abstract datatypes (not profiles!): if this schema is an abstract datatype,
            // we need to run validation against the schema for the actual type, not the abstract type.
            if (StructureDefinition.IsAbstract && StructureDefinition.Derivation != StructureDefinitionInformation.TypeDerivationRule.Constraint)
            {
                if (vc.ElementSchemaResolver is null)
                    throw new ArgumentException($"Cannot validate the resource because {nameof(ValidationSettings)} does not contain an ElementSchemaResolver.");

                if (input.InstanceType is null)
                {
                    throw new ArgumentException($"Cannot validate the resource because {nameof(IScopedNode)} does not have an instance type.");
                }

                var typeProfile = vc.TypeNameMapper.MapTypeName(input.InstanceType);
                var fetchResult = FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, state, typeProfile);
                return fetchResult.Success ? fetchResult.Schema!.ValidateInternal(input, vc, state) : fetchResult.Error!;
            }
            else
                return base.ValidateInternal(input, vc, state);

        }

        internal override async ValueTask<ResultReport> ValidateInternalAsync(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
        {
            // Schemas representing the root of a FHIR datatype cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var inputs = input.ToList();
            var results = new ResultReport[inputs.Count];
            for (int i = 0; i < inputs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results[i] = await ValidateInternalAsync(inputs[i], vc, state.UpdateInstanceLocation(d => d.ToIndex(i)), cancellationToken);
            }

            return ResultReport.Combine(results);
        }

        internal override async ValueTask<ResultReport> ValidateInternalAsync(IScopedNode input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
        {
            // FHIR specific rule about dealing with abstract datatypes (not profiles!): if this schema is an abstract datatype,
            // we need to run validation against the schema for the actual type, not the abstract type.
            if (StructureDefinition.IsAbstract && StructureDefinition.Derivation != StructureDefinitionInformation.TypeDerivationRule.Constraint)
            {
                if (vc.ElementSchemaResolver is null)
                    throw new ArgumentException($"Cannot validate the resource because {nameof(ValidationSettings)} does not contain an ElementSchemaResolver.");

                if (input.InstanceType is null)
                {
                    throw new ArgumentException($"Cannot validate the resource because {nameof(IScopedNode)} does not have an instance type.");
                }

                var typeProfile = vc.TypeNameMapper.MapTypeName(input.InstanceType);
                var fetchResult = await FhirSchemaGroupAnalyzer.FetchSchemaAsync(vc.ElementSchemaResolver, state, typeProfile);
                return fetchResult.Success ? await fetchResult.Schema!.ValidateInternalAsync(input, vc, state, cancellationToken) : fetchResult.Error!;
            }
            else
                return await base.ValidateInternalAsync(input, vc, state, cancellationToken);

        }

        /// <inheritdoc/>
        internal override string FhirSchemaKind => "datatype";
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
