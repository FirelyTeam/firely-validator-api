/* 
* Copyright (c) 2019, Firely (info@fire.ly) and contributors
* See the file CONTRIBUTORS for details.
* 
* This file is licensed under the BSD 3-Clause license
* available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
*/

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The interface for a validation assertion that validates a rule about a set of elements.
    /// </summary>
    /// <remarks>A rule that validates cardinality is a great example of this kind of assertion.</remarks>
    public interface IGroupValidatable : IAssertion
    {
        Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc);
    }
}
