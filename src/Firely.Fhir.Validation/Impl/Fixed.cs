/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element is exactly the same as a given fixed value.
    /// </summary>
    [DataContract]
    public class Fixed : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public ITypedElement FixedValue { get; private set; }
#else
        [DataMember]
        public ITypedElement FixedValue { get; private set; }
#endif


        public Fixed(ITypedElement fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
        }

        public Fixed(object fixedValue) : this(ElementNode.ForPrimitive(fixedValue)) { }

        public Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var result = Assertions.EMPTY;

            if (EqualityOperators.IsEqualTo(FixedValue, input) != true)
            {
                result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, input.Location, $"Value is not exactly equal to fixed value '{FixedValue.Value}'"));

                return Task.FromResult(result);
            }

            return Task.FromResult(Assertions.SUCCESS);
        }

        public JToken ToJson() => new JProperty($"Fixed[{FixedValue.InstanceType}]", FixedValue.ToPropValue());
    }
}
