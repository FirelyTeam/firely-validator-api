/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
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
    public class Fixed : SimpleAssertion
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

        public override string Key => "fixed[x]";

        public override object Value => FixedValue;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var result = Assertions.EMPTY;

            if (EqualityOperators.IsEqualTo(FixedValue, input) != true)
            {
                //TODO: we need a better ToString() for ITypedElement
                result += ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, input.Location,
                    $"Value '{input.Value}' is not exactly equal to fixed value '{FixedValue.Value}'"));

                return Task.FromResult(result);
            }

            return Task.FromResult(Assertions.SUCCESS);
        }

        public override JToken ToJson()
        {
            return new JProperty(Key, FixedValue.ToJObject());
        }
    }
}
