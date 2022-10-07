/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The interface for a validation assertion that validates a rule about a set of elements.
    /// </summary>
    /// <remarks>A rule that validates cardinality is a great example of this kind of assertion.</remarks>
    public interface IGroupValidatable : IAssertion, IValidatable
    {
        /// <summary>
        /// Validates a set of instances, given a location representative for the group.
        /// </summary>
        ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state);
    }
}
