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
    public static class ValidationExtensions
    {
        // Main entry point
        public static async Task<Assertions> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, ValidationContext vc)
            => await assertion.Validate(input.Select(i => i.asScopedNode()), vc, new ValidationState()).ConfigureAwait(false);

        // Main entry point
        public static async Task<Assertions> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc)
            => await assertion.Validate(input.asScopedNode(), vc, new ValidationState()).ConfigureAwait(false);

        private static ITypedElement asScopedNode(this ITypedElement node) => node is ScopedNode ? node : new ScopedNode(node);

        internal static async Task<Assertions> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(input, vc, state).ConfigureAwait(false),
                IValidatable validatable => await validatable.Downgrade(input, vc, state).ConfigureAwait(false),
                _ => Assertions.EMPTY,
            };
        }

        public static async Task<Assertions> Downgrade(this IValidatable assertion, IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            return input.ToList() switch
            {
                { Count: 0 } => Assertions.EMPTY,
                { Count: 1 } => await assertion.Validate(input.Single(), vc, state),
                _ => await input.Select(ma => assertion.Validate(ma, vc, state)).AggregateAssertions().ConfigureAwait(false)
            };
        }


        internal static async Task<Assertions> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc, ValidationState state) =>
            assertion switch
            {
                IValidatable validatable => await validatable.Validate(input, vc, state).ConfigureAwait(false),
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(new[] { input }, vc, state).ConfigureAwait(false),
                _ => Assertions.SUCCESS
            };

        internal async static Task<Assertions> AggregateAssertions(this IEnumerable<Task<Assertions>> tasks)
        {
            var result = await Task.WhenAll(tasks);
            return result.Aggregate(Assertions.EMPTY, (sum, other) => sum += other);
        }
    }
}