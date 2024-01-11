/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

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
        internal Canonical Id { get; private set; }

        /// <summary>
        /// The member assertions that constitute this schema.
        /// </summary>
        [DataMember]
        internal IReadOnlyCollection<IAssertion> Members { get; private set; }

        /// <summary>
        /// List of assertions to be performed first before the other statements. Failed assertions in this list will cause the 
        /// other members to fail to execute, which is a performance gain
        /// </summary>
        internal IReadOnlyCollection<IAssertion> ShortcutMembers { get; private set; }

        /// <summary>
        /// Lists the <see cref="CardinalityValidator"/> present in the members of this schema.
        /// </summary>
        internal IReadOnlyCollection<CardinalityValidator> CardinalityValidators { get; private set; } = Array.Empty<CardinalityValidator>();

        /// <inheritdoc cref="ElementSchema(Canonical, IEnumerable{IAssertion})"/>
        internal ElementSchema(Canonical id, params IAssertion[] members) : this(id, members.AsEnumerable())
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members and id.
        /// </summary>
        internal ElementSchema(Canonical id, IEnumerable<IAssertion> members)
        {
            Members = members.ToList();
            ShortcutMembers = extractShortcutMembers(Members);
            CardinalityValidators = Members.OfType<CardinalityValidator>().ToList();
            Id = id;
        }

        /// <summary>
        /// Extract all the shortcut members from the list of all member assertions. 
        /// </summary>
        /// <param name="members">The complete list of member assertions</param>
        /// <returns>List of shortcut member assertions</returns>
        private static IReadOnlyCollection<IAssertion> extractShortcutMembers(IEnumerable<IAssertion> members)
            => members.OfType<FhirTypeLabelValidator>().ToList();

        internal virtual ResultReport ValidateInternal(
            IEnumerable<IScopedNode> input,
            ValidationSettings vc,
            ValidationState state)
        {
            // If there is no input, just run the cardinality checks, nothing else - essential to keep validation performance high.
            if (!input.Any())
            {
                var nothing = Enumerable.Empty<IScopedNode>();

                if (!CardinalityValidators.Any())
                    return ResultReport.SUCCESS;
                else
                {
                    var validationResults = CardinalityValidators.Select(cv => cv.Validate(nothing, vc, state)).ToList();
                    return ResultReport.Combine(validationResults);
                }
            }

            var members = Members.Where(vc.Filter);
            var subresult = members.Select(ma => ma.ValidateMany(input, vc, state));
            return ResultReport.Combine(subresult.ToList());
        }


        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{IScopedNode}, ValidationSettings, ValidationState)"/>
        ResultReport IGroupValidatable.Validate(
            IEnumerable<IScopedNode> input,
            ValidationSettings vc,
            ValidationState state) => ValidateInternal(input, vc, state);

        internal virtual ResultReport ValidateInternal(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            // If we have shortcut members, run them first
            if (ShortcutMembers.Count != 0)
            {
                var subResult = ShortcutMembers.Where(vc.Filter).Select(ma => ma.ValidateOne(input, vc, state));
                var report = ResultReport.Combine(subResult.ToList());
                if (!report.IsSuccessful) return report;
            }

            var members = Members.Where(vc.Filter);
            var subresult = members.Select(ma => ma.ValidateOne(input, vc, state));
            return ResultReport.Combine(subresult.ToList());
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings vc, ValidationState state) => ValidateInternal(input, vc, state);

        /// <summary>
        /// Lists additional properties shown as metadata on the schema, separate from the members.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<JProperty> MetadataProps() => Enumerable.Empty<JProperty>();

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public virtual JToken ToJson()
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

            var metadataProps = MetadataProps().ToList();
            if (metadataProps.Any())
                result.Add(new JProperty(".metadata", new JObject(metadataProps)));

            var properties = uniqueMembers.Select(um => new JProperty(um.pn, um.pv));
            foreach (var property in properties) result.Add(property);

            return result;
        }

        /// <summary>
        /// Find the first subschema with the given anchor.
        /// </summary>
        /// <returns>An <see cref="ElementSchema"/> if found, otherwise <c>null</c>.</returns>
        internal ElementSchema? FindFirstByAnchor(string anchor) =>
            Members.OfType<DefinitionsAssertion>().Select(da => da.FindFirstByAnchor(anchor)).FirstOrDefault(s => s is not null);

        /// <summary>
        /// Whether the schema has members.
        /// </summary>
        internal bool IsEmpty() => !Members.Any();
    }
}
