/* 
 * Copyright (c) 2020, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class XmlOrder : SimpleAssertion
    {
        public readonly int Order;

        public XmlOrder(int order)
        {
            Order = order;
        }

        public override string Key => "xml-order";

        public override object Value => Order;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
            => Task.FromResult(Assertions.Success);
    }
}
