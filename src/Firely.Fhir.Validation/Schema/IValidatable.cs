/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implemented by assertions that work on a single ITypedElement.
    /// </summary>
    public interface IValidatable : IAssertion
    {
        Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState state);
    }
}
