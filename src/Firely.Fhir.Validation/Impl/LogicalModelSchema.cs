/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR logical model (<c>StructureDefinition.kind = "logical"</c>).
    /// </summary>
    [DataContract]
    public class LogicalModelSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="LogicalModelSchema"/>
        /// </summary>
        public LogicalModelSchema(StructureDefinitionInformation structureDefinition, params IAssertion[] members) : base(structureDefinition, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="LogicalModelSchema"/>
        /// </summary>
        public LogicalModelSchema(StructureDefinitionInformation structureDefinition, IEnumerable<IAssertion> members) : base(structureDefinition, members)
        {
            // nothing
        }

        /// <inheritdoc/>
        internal override ElementSchema WithMembers(IEnumerable<IAssertion> members)
            => new LogicalModelSchema(StructureDefinition, members);

        /// <inheritdoc />
        internal override ResultReport ValidateInternal(IEnumerable<PocoNode> input, ValidationSettings vc, ValidationState state)
        {
            // Schemas representing the root of a logical model cannot meaningfully be used as a GroupValidatable,
            // so we'll turn this into a normal IValidatable.
            var results = input.Select(i => ValidateInternal(i, vc, state));
            return ResultReport.Combine(results.ToList());
        }

        /// <inheritdoc />
        internal override ResultReport ValidateInternal(PocoNode input, ValidationSettings vc, ValidationState state) =>
            // Unlike DatatypeSchema, a logical model's declared "type" is a canonical url (e.g.
            // http://hl7.org/fhir/tools/StructureDefinition/CDSHooksRequest), not a FHIR type name that
            // can be compared against the instance's runtime type name - there is no abstract-type
            // indirection to perform here, just validate the members directly.
            base.ValidateInternal(input, vc, state);

        /// <inheritdoc/>
        internal override string FhirSchemaKind => "logical";
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
