/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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