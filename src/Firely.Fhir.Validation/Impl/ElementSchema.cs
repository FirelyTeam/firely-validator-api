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
    /// Represents a group of (sibling) rules that must all be succesful for the whole
    /// schema to be succesful.
    /// </summary>
    [DataContract]
    public class ElementSchema : IMergeable, IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public Uri Id { get; private set; }

        [DataMember(Order = 1)]
        public IEnumerable<IAssertion> Members => _members;
#else
        [DataMember]
        public Uri Id { get; private set; }

        [DataMember]
        public IEnumerable<IAssertion> Members => _members;
#endif

        private readonly Assertions _members;

        public ElementSchema(Assertions members) : this(members, buildUri(Guid.NewGuid().ToString()))
        {
            // TODO: should we do this, so we have always an Id?
        }

        public ElementSchema(Assertions members, Uri id)
        {
            _members = members;
            Id = id;
        }


        public ElementSchema(params IAssertion[] members) : this(members.AsEnumerable()) { }

        public ElementSchema(IEnumerable<IAssertion>? members) : this(new Assertions(members)) { }

        public ElementSchema(Uri id, params IAssertion[] members) : this(members) => Id = id;

        public ElementSchema(Uri id, IEnumerable<IAssertion>? members) : this(members) => Id = id;

        public ElementSchema(Uri id, Assertions members) : this(members) => Id = id;

        private static Uri buildUri(string uri) => new(uri, UriKind.RelativeOrAbsolute);

        public ElementSchema(string id, params IAssertion[] members) : this(members) => Id = buildUri(id);

        public ElementSchema(string id, IEnumerable<IAssertion> members) : this(members) => Id = buildUri(id);

        public ElementSchema(string id, Assertions members) : this(members) => Id = buildUri(id);

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            var members = _members.Where(vc.Filter);

            var multiAssertions = members.OfType<IGroupValidatable>();
            var singleAssertions = members.OfType<IValidatable>();

            var multiResults = await multiAssertions
                                        .Select(ma => ma.Validate(input, vc, state)).AggregateAsync();

            var singleResult = await input.Select(elt => singleAssertions.ValidateAsync(elt, vc, state)).AggregateAsync();
            return multiResults + singleResult;


            //TODO: can we do this as well? Makes it a bit shorter..
            //return await members.Select(m => m.Validate(input, vc)).AggregateAsync();
        }

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
            if (Id != null) result.Add(new JProperty("$id", Id.ToString()));
            var properties = uniqueMembers.Select(um => new JProperty(um.pn, um.pv));
            foreach (var property in properties) result.Add(property);

            return result;
        }

        public IMergeable Merge(IMergeable other) =>
            other is ElementSchema schema ? new ElementSchema(this._members + schema._members)
                : throw Error.InvalidOperation($"Internal logic failed: tried to merge an ElementSchema with a {other.GetType().Name}");
    }
}
