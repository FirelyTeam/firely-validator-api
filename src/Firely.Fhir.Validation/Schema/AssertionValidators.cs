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
    /// Defines a set of extension methods which enables starting validation using on any
    /// <see cref="IAssertion"/>.
    /// </summary>
    /// <remarks>There are basically three kinds of implementers of IAssertion: Implementors of 
    /// <see cref="IGroupValidatable"/>, implementors of <see cref="IValidatable"/> and implementers
    /// of <see cref="IAssertion"/> that do not implement these two interfaces. These extension
    /// methods allow the caller to invoke each of them, using a uniform Validate method call.
    /// </remarks>
    public static class AssertionValidators
    {
        /// <summary>
        /// Validates a set of instance elements against an assertion.
        /// </summary>
        public static async Task<ResultAssertion> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc)
            => await assertion.ValidateMany(input.Select(i => i.asScopedNode()), groupLocation, vc, new ValidationState()).ConfigureAwait(false);

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        public static async Task<ResultAssertion> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc)
            => await assertion.ValidateOne(input.asScopedNode(), vc, new ValidationState()).ConfigureAwait(false);

        private static ITypedElement asScopedNode(this ITypedElement node) => node is ScopedNode ? node : new ScopedNode(node);

        /// <summary>
        /// Validates a group of instance elements using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IGroupValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will call the validation on the assertion for
        /// each of the instances in the group and combine the result.</remarks>
        internal static async Task<ResultAssertion> ValidateMany(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(input, groupLocation, vc, state).ConfigureAwait(false),
                IValidatable validatable => await validatable.Repeat(input, groupLocation, vc, state).ConfigureAwait(false),
                _ => ResultAssertion.SUCCESS,
            };
        }

        /// <summary>
        /// Validates a single instance element using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will wrap the single instance as a group
        /// and call validation for the <see cref="IGroupValidatable"/>.</remarks>
        internal static async Task<ResultAssertion> ValidateOne(this IAssertion assertion, ITypedElement input, ValidationContext vc, ValidationState state) =>
            assertion switch
            {
                IValidatable validatable => await validatable.Validate(input, vc, state).ConfigureAwait(false),
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(new[] { input }, input.Location, vc, state).ConfigureAwait(false),
                _ => ResultAssertion.SUCCESS
            };

        /// <summary>
        /// Turn the validation of a group of elements using a <see cref="IGroupValidatable"/> into
        /// a sequence of calls of each element in the group against a <see cref="IValidatable"/>, and
        /// then combines the results of each of these calls.
        /// </summary>
        internal static async Task<ResultAssertion> Repeat(this IValidatable assertion, IEnumerable<ITypedElement> input, string _, ValidationContext vc, ValidationState state)
        {
            return input.ToList() switch
            {
                { Count: 0 } => ResultAssertion.SUCCESS,
                { Count: 1 } => await assertion.Validate(input.Single(), vc, state),
                _ => await input.Select(ma => assertion.Validate(ma, vc, state)).AggregateAssertions().ConfigureAwait(false)
            };
        }

        /// <summary>
        /// Awaits a list of validation tasks and combines the results into a single <see cref="ResultAssertion"/>.
        /// </summary>
        internal async static Task<ResultAssertion> AggregateAssertions(this IEnumerable<Task<ResultAssertion>> tasks)
        {
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            return ResultAssertion.FromEvidence(result);
        }
    }
}