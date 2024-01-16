/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// The base interface for an assertion.
    /// </summary>
    /// <remarks>Assertions are both input to the validator (in the form of <see cref="IValidatable"/> assertions) as output
    /// of the validator (<see cref="ResultReport"/>).</remarks>
    public interface IAssertion : IJsonSerializable
    {
    }
}