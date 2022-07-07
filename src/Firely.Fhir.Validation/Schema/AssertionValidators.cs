/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

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
        public static ResultAssertion Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc)
            => assertion.ValidateMany(input.Select(i => i.asScopedNode()), groupLocation, vc, new ValidationState());

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        public static ResultAssertion Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc)
            => assertion.ValidateOne(input.asScopedNode(), vc, new ValidationState());

        private static ITypedElement asScopedNode(this ITypedElement node) => node is ScopedNode ? node : new ScopedNode(node);

        /// <summary>
        /// Validates a group of instance elements using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IGroupValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will call the validation on the assertion for
        /// each of the instances in the group and combine the result.</remarks>
        internal static ResultAssertion ValidateMany(this IAssertion assertion, IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => groupvalidatable.Validate(input, groupLocation, vc, state),
                IValidatable validatable => repeat(validatable, input, vc, state),
                _ => ResultAssertion.SUCCESS,
            };

            // Turn the validation of a group of elements using a <see cref="IGroupValidatable"/> into
            // a sequence of calls of each element in the group against a <see cref="IValidatable"/>, and
            // then combines the results of each of these calls.
            static ResultAssertion repeat(IValidatable assertion, IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
            {
                return input.ToList() switch
                {
                    { Count: 0 } => ResultAssertion.SUCCESS,
                    { Count: 1 } => assertion.Validate(input.Single(), vc, state),
                    _ => ResultAssertion.FromEvidence(input.Select(ma => assertion.Validate(ma, vc, state)).ToList())
                };
            }
        }

        /// <summary>
        /// Validates a single instance element using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will wrap the single instance as a group
        /// and call validation for the <see cref="IGroupValidatable"/>.</remarks>
        internal static ResultAssertion ValidateOne(this IAssertion assertion, ITypedElement input, ValidationContext vc, ValidationState state) =>
            assertion switch
            {
                IValidatable validatable => validatable.Validate(input, vc, state),
                _ => ResultAssertion.SUCCESS
            };
    }
}