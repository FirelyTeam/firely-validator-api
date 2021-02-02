/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents an assertion that can be merged with other comparable assertions.
    /// </summary>
    /// <remarks>For example, a Cardinality assertion of 0..* and 1..2 would be "merged" into a single
    /// new rule with a cardinality of 1..2.</remarks>
    public interface IMergeable
    {
        IMergeable Merge(IMergeable other);
    }
}
