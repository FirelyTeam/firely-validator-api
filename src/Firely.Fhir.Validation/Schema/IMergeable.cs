/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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
