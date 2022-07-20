/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Hl7.FhirPath.Sprache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public class ResourceSchema : ElementSchema
    {
        //[DataMember]
        //public ElementSchema Definition { get; private set; }

        public ResourceSchema(params IAssertion[] members) : base(members.AsEnumerable())
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(IAssertion[])"/>
        public ResourceSchema(IEnumerable<IAssertion> members) : base(new Canonical(Guid.NewGuid().ToString()), members)
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(Canonical, IEnumerable{IAssertion})"/>
        public ResourceSchema(Canonical id, params IAssertion[] members) : base(id, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members and id.
        /// </summary>
        public ResourceSchema(Canonical id, IEnumerable<IAssertion> members) : base(id, members)
        {
            // nothing
        }


        /// <inheritdoc />
        public override JToken ToJson() => new JObject(
            new JProperty("type", "resource"),
            new JProperty("id", Id.ToString()),
            new JProperty("nested", base.ToJson()));

        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var results = input.Select(i => Validate(i, vc, state));
            return ResultReport.FromEvidence(results.ToList());
        }

        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state) =>
             state.Global.ExternalValidations.Start(
             input.GetUrlAndPath(),
             Id.ToString(),
             () => base.Validate(input, vc, state));
    }
}
