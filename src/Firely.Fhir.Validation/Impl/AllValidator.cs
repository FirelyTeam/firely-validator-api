/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion that expresses that all member assertions should hold.
    /// </summary>
    [DataContract]
    public class AllValidator : IGroupValidatable
    {
#if MSGPACK_KEY
        /// <summary>
        /// The member assertions the instance should be validated against.
        /// </summary>
        [DataMember(Order = 0)]      
        public IAssertion[] Members { get; private set; }
#else
        /// <summary>
        /// The member assertions the instance should be validated against.
        /// </summary>
        [DataMember]
        public IAssertion[] Members { get; private set; }
#endif

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AllValidator(IEnumerable<IAssertion> members)
        {
            Members = members.ToArray();
        }

        /// <summary>
        /// Construct an <see cref="AllValidator"/> based on its members.
        /// </summary>
        /// <param name="members"></param>
        public AllValidator(params IAssertion[] members) : this(members.AsEnumerable())
        {
        }

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, ValidationContext, ValidationState)"/>
        public async Task<Assertions> Validate(
            IEnumerable<ITypedElement> input,
            ValidationContext vc,
            ValidationState state) =>
                await Members
                    .Select(ma => ma.Validate(input, vc, state))
                    .AggregateAssertions()
                    .ConfigureAwait(false);

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() =>
            new JProperty("allOf", new JArray(Members.Select(m => new JObject(m.ToJson()))));
    }
}
