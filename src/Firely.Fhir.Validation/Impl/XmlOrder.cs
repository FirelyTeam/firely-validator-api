/* 
 * Copyright (c) 2020, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the order of elements when the data originated from XML.
    /// </summary>
    [DataContract]
    public class XmlOrder : SimpleAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int Order { get; private set; }
#else
        [DataMember]
        public int Order { get; private set; }
#endif

        public XmlOrder(int order)
        {
            Order = order;
        }

        public override string Key => "xml-order";

        public override object Value => Order;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
            => Task.FromResult(Assertions.SUCCESS);
    }
}
