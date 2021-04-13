/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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
    public class XmlOrderValidator : BasicValidator
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int Order { get; private set; }
#else
        [DataMember]
        public int Order { get; private set; }
#endif

        public XmlOrderValidator(int order)
        {
            Order = order;
        }

        public override string Key => "xml-order";

        public override object Value => Order;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext _, ValidationState __)
            => Task.FromResult(Assertions.SUCCESS);
    }
}
