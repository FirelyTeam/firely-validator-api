/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Implemented by assertions that work on a single ITypedElement.
    /// </summary>
    public interface IValidatable : IAssertion
    {
        Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state);
    }

    public static class IValidatableExtensions
    {
        public async static Task<Assertions> ValidateAsync(this IEnumerable<IValidatable> validatables, ITypedElement elt, ValidationContext vc, ValidationState state)
        {
            return await validatables.Select(v => v.Validate(elt, vc, state)).AggregateAsync();
        }

        public async static Task<Assertions> AggregateAsync(this IEnumerable<Task<Assertions>> tasks)
        {
            var result = await Task.WhenAll(tasks);
            return result.Aggregate(Assertions.EMPTY, (sum, other) => sum += other);
        }
    }
}
