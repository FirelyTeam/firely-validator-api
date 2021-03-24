/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
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
    public class ElementSchema : IElementSchema, IMergeable
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

        private static Uri buildUri(string uri) => new Uri(uri, UriKind.RelativeOrAbsolute);

        public ElementSchema(string id, params IAssertion[] members) : this(members) => Id = buildUri(id);

        public ElementSchema(string id, IEnumerable<IAssertion> members) : this(members) => Id = buildUri(id);

        public ElementSchema(string id, Assertions members) : this(members) => Id = buildUri(id);

        public bool IsEmpty => !_members.Any();

        public async Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var members = _members.Where(vc.Filter);

            var multiAssertions = members.OfType<IGroupValidatable>();
            var singleAssertions = members.OfType<IValidatable>();

            var multiResults = await multiAssertions
                                        .Select(ma => ma.Validate(input, vc)).AggregateAsync();

            var singleResult = await input.Select(elt => singleAssertions.ValidateAsync(elt, vc)).AggregateAsync();
            return multiResults + singleResult;


            //TODO: can we do this as well? Makes it a bit shorter..
            //return await members.Select(m => m.Validate(input, vc)).AggregateAsync();
        }

        public JToken ToJson()
        {
            var result = new JObject();
            if (Id != null) result.Add(new JProperty("$id", Id.ToString()));
            result.Add(_members.Select(mem => nest(mem.ToJson())));
            return result;

            static JToken nest(JToken mem) =>
                mem is JObject ? new JProperty("nested", mem) : mem;
        }

        public IMergeable Merge(IMergeable other) =>
            other is ElementSchema schema ? new ElementSchema(this._members + schema._members)
                : throw Error.InvalidOperation($"Internal logic failed: tried to merge an ElementSchema with a {other.GetType().Name}");
    }
}
