/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

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
}