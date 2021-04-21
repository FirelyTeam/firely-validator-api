/* 
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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a group of member rules that must all be succesful for the whole
    /// schema to be succesful.
    /// </summary>
    [DataContract]
    public class ElementSchema : IMergeable, IGroupValidatable
    {
#if MSGPACK_KEY
        /// <summary>
        /// The unique id for this schema.
        /// </summary>
        [DataMember(Order = 0)]
        public Uri Id { get; private set; }

        /// <summary>
        /// The member assertions that constitute this schema.
        /// </summary>
        [DataMember(Order = 1)]
        public IEnumerable<IAssertion> Members => _members;
#else
        /// <summary>
        /// The unique id for this schema.
        /// </summary>
        [DataMember]
        public Uri Id { get; private set; }

        /// <summary>
        /// The member assertions that constitute this schema.
        /// </summary>
        [DataMember]
        public IEnumerable<IAssertion> Members => _members;
#endif

        private readonly Assertions _members;

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members. The schema will be given an unqiue
        /// generated id.
        /// </summary>
        public ElementSchema(params IAssertion[] members) : this(members.AsEnumerable()) { }

        /// <inheritdoc cref="ElementSchema(IAssertion[])"/>
        public ElementSchema(IEnumerable<IAssertion> members) : this(new Assertions(members)) { }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members and id.
        /// </summary>
        public ElementSchema(Uri id, params IAssertion[] members) : this(id, members.AsEnumerable())
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(Uri, IAssertion[])"/>
        public ElementSchema(Uri id, IEnumerable<IAssertion> members) : this(id, new Assertions(members))
        {
            // nothing
        }

        private static Uri buildUri(string uri) => new(uri, UriKind.RelativeOrAbsolute);

        /// <inheritdoc cref="ElementSchema(Uri, IAssertion[])"/>
        public ElementSchema(string id, params IAssertion[] members) : this(buildUri(id), members.AsEnumerable())
        {
            // nothing
        }

        /// <inheritdoc cref="ElementSchema(Uri, IAssertion[])"/>
        public ElementSchema(string id, IEnumerable<IAssertion> members) : this(buildUri(id), members)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members and id.
        /// </summary>
        public ElementSchema(Uri id, Assertions members)
        {
            _members = members;
            Id = id;
        }

        /// <inheritdoc cref="ElementSchema.ElementSchema(Uri, Assertions)"/>
        public ElementSchema(string id, Assertions members) : this(buildUri(id), members)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ElementSchema"/> with the given members. The schema will be given an unqiue
        /// generated id.
        /// </summary>
        public ElementSchema(Assertions members) : this(buildUri(Guid.NewGuid().ToString()), members)
        {
            // nothing
        }

        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationContext, ValidationState)"/>
        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var members = _members.Where(vc.Filter);
            return await members.Select(ma => ma.Validate(input, groupLocation, vc, state)).AggregateAssertions().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            static JToken nest(JToken mem) =>
                mem is JObject ? new JProperty("nested", mem) : mem;

            var members = _members.Select(mem => nest(mem.ToJson())).OfType<JProperty>()
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

        /// <inheritdoc cref="IMergeable.Merge(IMergeable)"/>
        public IMergeable Merge(IMergeable other) =>
            other is ElementSchema schema ? new ElementSchema(this._members + schema._members)
                : throw Error.InvalidOperation($"Internal logic failed: tried to merge an ElementSchema with a {other.GetType().Name}");

        /// <summary>
        /// Find the first subschema with the given anchor.
        /// </summary>
        /// <returns>An <see cref="ElementSchema"/> if found, otherwise <c>null</c>.</returns>
        public ElementSchema FindFirstByAnchor(string anchor) =>
            _members.OfType<DefinitionsAssertion>().Select(da => da.FindFirstByAnchor(anchor)).FirstOrDefault(s => s is not null);
    }
}
