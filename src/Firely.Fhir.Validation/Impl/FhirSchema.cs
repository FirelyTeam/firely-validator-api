﻿/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR datatype or resource.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public abstract class FhirSchema : ElementSchema
    {
        /// <summary>
        /// A collection of information from the StructureDefintion from which
        /// this schema was generated.
        /// </summary>
        [DataMember]
        internal StructureDefinitionInformation StructureDefinition { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="FhirSchema"/>
        /// </summary>
        public FhirSchema(StructureDefinitionInformation structureDefinition, params IAssertion[] members) : this(structureDefinition, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="FhirSchema"/>
        /// </summary>
        public FhirSchema(StructureDefinitionInformation structureDefinition, IEnumerable<IAssertion> members) : base(structureDefinition.Canonical, members)
        {
            StructureDefinition = structureDefinition;
        }

        /// <inheritdoc/>
        internal override ResultReport ValidateInternal(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            if (input.InstanceType is null)
                throw new ArgumentException($"Cannot validate the resource because {nameof(IScopedNode)} does not have an instance type.");

            state = state
                .UpdateLocation(sp => sp.InvokeSchema(this))
                .UpdateInstanceLocation(ip => ip.StartResource(input.InstanceType));
            return base.ValidateInternal(input, vc, state);
        }

        /// <inheritdoc/>
        internal override ResultReport ValidateInternal(IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state)
        {
            state = state.UpdateLocation(sp => sp.InvokeSchema(this));
            return base.ValidateInternal(input, vc, state);
        }

        /// <summary>
        /// The kind of schema we are defining. Used to distinguish the subclasses of <see cref="FhirSchema"/>
        /// in the Json rendering.
        /// </summary>
        internal abstract string FhirSchemaKind { get; }

        /// <inheritdoc />
        internal override IEnumerable<JProperty> MetadataProps()
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
        
        internal IEnumerable<string> GetOwnAndBaseTypes()
        {
            return (StructureDefinition.BaseCanonicals ?? Enumerable.Empty<Canonical>())
                .Append(StructureDefinition.Canonical)
                .Select(canonical => canonical.Uri!.Split('/').Last());
        }
    }
}
