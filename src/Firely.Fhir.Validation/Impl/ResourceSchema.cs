/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR Resource.
    /// </summary>
    /// <remarks>It will perform additional resource-specific validation logic associated with resources,
    /// like selecting Meta.profile as additional profiles to be validated.</remarks>
    public class ResourceSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation sdi, params IAssertion[] members) : base(sdi, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ResourceSchema"/>
        /// </summary>
        public ResourceSchema(StructureDefinitionInformation sdi, IEnumerable<IAssertion> members) : base(sdi, members)
        {
            // nothing
        }

        /// <inheritdoc />
        protected override Canonical[] GetAdditionalSchemas(ITypedElement instance) =>
            instance
                .Children("meta")
                .Children("profile")
                .Select(ite => ite.Value)
                .OfType<string>()
                .Select(s => new Canonical(s))
                .ToArray();

        /// <inheritdoc />
        protected override ResultReport ValidateConstraints(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var resourceUrl = state.Instance.ExternalUrl;
            var fullLocation = (resourceUrl is not null ? resourceUrl + "#" : "") + input.Location;

            return state.Global.RunValidations.Start(
                fullLocation,
                Id.ToString(),  // is the same as the canonical for resource schemas
                () =>
                {
                    state.Global.ResourcesValidated += 1;
                    return base.ValidateConstraints(input, vc, state);
                });
        }

        /// <inheritdoc/>
        protected override string FhirSchemaKind => "resource";
    }
}
