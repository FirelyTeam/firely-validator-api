/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// The base interface for an assertion.
    /// </summary>
    /// <remarks>Assertions are both input to the validator (in the form of <see cref="IValidatable"/> assertions) as output
    /// of the validator (<see cref="ResultAssertion"/>).</remarks>
    public interface IAssertion : IJsonSerializable
    {
    }

    /// <summary>
    /// Represents an object that can represent its internal configuration as Json.
    /// </summary>
    public interface IJsonSerializable
    {
        JToken ToJson();
    }
}