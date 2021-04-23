/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public static class ValidationExtensions
    {
        // Main entry point
        public static async Task<ResultAssertion> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc)
            => await assertion.Validate(input.Select(i => i.asScopedNode()), groupLocation, vc, new ValidationState()).ConfigureAwait(false);

        // Main entry point
        public static async Task<ResultAssertion> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc)
            => await assertion.Validate(input.asScopedNode(), vc, new ValidationState()).ConfigureAwait(false);

        private static ITypedElement asScopedNode(this ITypedElement node) => node is ScopedNode ? node : new ScopedNode(node);

        internal static async Task<ResultAssertion> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(input, groupLocation, vc, state).ConfigureAwait(false),
                IValidatable validatable => await validatable.Downgrade(input, groupLocation, vc, state).ConfigureAwait(false),
                _ => ResultAssertion.SUCCESS,
            };
        }

        public static async Task<ResultAssertion> Downgrade(this IValidatable assertion, IEnumerable<ITypedElement> input, string _, ValidationContext vc, ValidationState state)
        {
            return input.ToList() switch
            {
                { Count: 0 } => ResultAssertion.SUCCESS,
                { Count: 1 } => await assertion.Validate(input.Single(), vc, state),
                _ => await input.Select(ma => assertion.Validate(ma, vc, state)).AggregateAssertions().ConfigureAwait(false)
            };
        }


        internal static async Task<ResultAssertion> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc, ValidationState state) =>
            assertion switch
            {
                IValidatable validatable => await validatable.Validate(input, vc, state).ConfigureAwait(false),
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(new[] { input }, input.Location, vc, state).ConfigureAwait(false),
                _ => ResultAssertion.SUCCESS
            };

        internal async static Task<ResultAssertion> AggregateAssertions(this IEnumerable<Task<ResultAssertion>> tasks)
        {
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            return ResultAssertion.FromEvidence(result);
        }

        public static ValidationResult Combine(this ValidationResult a, ValidationResult b) =>
            (a, b) switch
            {
                (_, ValidationResult.Failure) => ValidationResult.Failure,
                (ValidationResult.Failure, _) => ValidationResult.Failure,
                (ValidationResult.Undecided, _) => ValidationResult.Undecided,
                (ValidationResult.Success, var other) => other,
                _ => throw new NotSupportedException($"Enum values have been added to {nameof(ValidationResult)} " +
                    $"and {nameof(ValidationExtensions.Combine)}() should be adapted accordingly.")
            };
    }
}