﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a group of member rules that must all be succesful for the whole
    /// schema to be succesful.
    /// </summary>
    [DataContract]
    public class ElementSchema : IGroupValidatable
    {
        /// <summary>
        /// The unique id for this schema.
        /// </summary>
        [DataMember]
        public Canonical Id { get; private set; }

        /// <summary>
        /// The member assertions that constitute this schema.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<IAssertion> Members { get; private set; }

        /// <summary>
        /// Lists the <see cref="CardinalityValidator"/> present in the members of this schema.
        /// </summary>
        public IReadOnlyCollection<CardinalityValidator> CardinalityValidators { get; private set; } = Array.Empty<CardinalityValidator>();

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members. The schema will be given an unqiue
        /// generated id.
        /// </summary>
        public ElementSchema(params IAssertion[] members) : this(members.AsEnumerable())
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(IAssertion[])"/>
        public ElementSchema(IEnumerable<IAssertion> members) : this(new Canonical(Guid.NewGuid().ToString()), members)
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(Canonical, IEnumerable{IAssertion})"/>
        public ElementSchema(Canonical id, params IAssertion[] members) : this(id, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members and id.
        /// </summary>
        public ElementSchema(Canonical id, IEnumerable<IAssertion> members)
        {
            Members = members.ToList();
            CardinalityValidators = Members.OfType<CardinalityValidator>().ToList();
            Id = id;
        }


        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, string, ValidationContext, ValidationState)"/>
        public ResultAssertion Validate(
            IEnumerable<ITypedElement> input,
            string groupLocation,
            ValidationContext vc,
            ValidationState state)
        {
            // If there is no input, just run the cardinality checks, nothing else - essential to keep validation performance high.
            if (!input.Any())
            {
                var nothing = Enumerable.Empty<ITypedElement>();

                if (!CardinalityValidators.Any())
                    return ResultAssertion.SUCCESS;
                else
                {
                    var validationResults = CardinalityValidators.Select(cv => cv.Validate(nothing, groupLocation, vc, state)).ToList();
                    return ResultAssertion.FromEvidence(validationResults);
                }
            }

            var members = Members.Where(vc.Filter);
            var subresult = members.Select(ma => ma.ValidateMany(input, groupLocation, vc, state));
            return ResultAssertion.FromEvidence(subresult.ToList());
        }

        /// <inheritdoc />
        public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var members = Members.Where(vc.Filter);
            var subresult = members.Select(ma => ma.ValidateOne(input, vc, state));
            return ResultAssertion.FromEvidence(subresult.ToList());
        }


        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            static JToken nest(JToken mem) =>
                mem is JObject ? new JProperty("nested", mem) : mem;

            var members = Members.Select(mem => nest(mem.ToJson())).OfType<JProperty>()
                .Select(prop => (name: prop.Name, value: prop.Value)).ToList();

            // members is a collection of (name, JToken) pairs, where name may have duplicates.
            // since we want to add these pairs as properties to an object, we need to make
            // sure the names are unique (there might be multiple assertions of the same kind
            // in the group after all!). So, group these by name, then ungroup them again by
            // SelectMany(), in the mean time adding the index number of a repeat within a group
            // so the first in the group gets the original name, and the Nth in the group gets
            // its name suffixed by the N.
            var uniqueMembers = members.GroupBy(prop => prop.name)
                .SelectMany(grp => grp.Select((gi, index) => (pn: index == 0 ? gi.name : gi.name + (index + 1), pv: gi.value)));

            // Now, uniqueMembers are pairs of (name, JToken) again, but name is unique. We
            // can now construct JProperties from them.
            var result = new JObject();
            if (Id != null) result.Add(new JProperty("id", Id.ToString()));
            var properties = uniqueMembers.Select(um => new JProperty(um.pn, um.pv));
            foreach (var property in properties) result.Add(property);

            return result;
        }

        /// <summary>
        /// Find the first subschema with the given anchor.
        /// </summary>
        /// <returns>An <see cref="ElementSchema"/> if found, otherwise <c>null</c>.</returns>
        public ElementSchema FindFirstByAnchor(string anchor) =>
            Members.OfType<DefinitionsAssertion>().Select(da => da.FindFirstByAnchor(anchor)).FirstOrDefault(s => s is not null);

        /// <summary>
        /// Whether the schema has members.
        /// </summary>
        public bool IsEmpty() => !Members.Any();
    }
}
