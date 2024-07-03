/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public static class AssertionValidators
    {
        /// <summary>
        /// Validates a set of instance elements against an assertion.
        /// </summary>
        internal static ResultReport Validate(this IAssertion assertion, IEnumerable<IScopedNode> input, ValidationSettings vc)
            => assertion.ValidateMany(input, vc, new ValidationState());

        /// <summary>
        /// Validates a set of instance elements against an assertion.
        /// </summary>
        public static ResultReport Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, ValidationSettings vc)
            => assertion.ValidateMany(input.Select(i => i.AsScopedNode()), vc, new ValidationState());

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        internal static ResultReport Validate(this IAssertion assertion, IScopedNode input, ValidationSettings vc)
            => assertion.ValidateOne(input, vc, new ValidationState());

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        internal static ValueTask<ResultReport> ValidateAsync(this IAssertion assertion, IScopedNode input, ValidationSettings vc, CancellationToken cancellationToken)
            => assertion.ValidateOneAsync(input, vc, new ValidationState(), cancellationToken);

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        internal static ValueTask<ResultReport> ValidateAsync(this IAssertion assertion, ITypedElement input, ValidationSettings vc, CancellationToken cancellationToken)
            => assertion.ValidateOneAsync(input.AsScopedNode(), vc, new ValidationState(), cancellationToken);

        /// <summary>
        /// Validates a single instance element against an assertion.
        /// </summary>
        public static ResultReport Validate(this IAssertion assertion, ITypedElement input, ValidationSettings vc)
            => assertion.ValidateOne(input.AsScopedNode(), vc, new ValidationState());

        /// <summary>
        /// Validates a group of instance elements using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IGroupValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will call the validation on the assertion for
        /// each of the instances in the group and combine the result.</remarks>
        internal static ResultReport ValidateMany(this IAssertion assertion, IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => groupvalidatable.Validate(input, vc, state),
                IValidatable validatable => repeat(validatable, input, vc, state),
                _ => ResultReport.SUCCESS,
            };

            // Turn the validation of a group of elements using a <see cref="IGroupValidatable"/> into
            // a sequence of calls of each element in the group against a <see cref="IValidatable"/>, and
            // then combines the results of each of these calls.
            static ResultReport repeat(IValidatable assertion, IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state)
            {
                return input.ToList() switch
                {
                    { Count: 0 } => ResultReport.SUCCESS,
                    { Count: 1 } when input.Single() is ValueElementNode ve => assertion.Validate(ve, vc, state), // no index for ValueElementNode
                    { Count: 1 } => assertion.Validate(input.Single(), vc, state.UpdateInstanceLocation(vs => vs.ToIndex(0))),
                    _ => ResultReport.Combine(input.Select((ma, i) => assertion.Validate(ma, vc, state.UpdateInstanceLocation(vs => vs.ToIndex(i)))).ToList())
                };
            }
        }

        /// <summary>
        /// Validates a group of instance elements using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IGroupValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will call the validation on the assertion for
        /// each of the instances in the group and combine the result.</remarks>
        internal static ValueTask<ResultReport> ValidateManyAsync(this IAssertion assertion, IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
        {
            return assertion switch
            {
                IGroupValidatable groupvalidatable => groupvalidatable.ValidateAsync(input, vc, state, cancellationToken),
                IValidatable validatable => repeat(validatable, input, vc, state, cancellationToken),
                _ => new(ResultReport.SUCCESS),
            };

            // Turn the validation of a group of elements using a <see cref="IGroupValidatable"/> into
            // a sequence of calls of each element in the group against a <see cref="IValidatable"/>, and
            // then combines the results of each of these calls.
            static ValueTask<ResultReport> repeat(IValidatable assertion, IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
            {
                return input.ToList() switch
                {
                    { Count: 0 } => new(ResultReport.SUCCESS),
                    { Count: 1 } when input.Single() is ValueElementNode ve => assertion.ValidateAsync(ve, vc, state, cancellationToken), // no index for ValueElementNode
                    { Count: 1 } => assertion.ValidateAsync(input.Single(), vc, state.UpdateInstanceLocation(vs => vs.ToIndex(0)), cancellationToken),
                    _ => combined(assertion, input, vc, state, cancellationToken)
                };
            }

            async static ValueTask<ResultReport> combined(IValidatable assertion, IEnumerable<IScopedNode> input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken)
            {
                var inputs = input.ToList();
                var reports = new ResultReport[inputs.Count];
                for (var i = 0; i < inputs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = inputs[i];
                    reports[i] = await assertion.ValidateAsync(item, vc, state.UpdateInstanceLocation(vs => vs.ToIndex(i)), cancellationToken);
                }

                return ResultReport.Combine(reports);
            }
        }

        /// <summary>
        /// Validates a single instance element using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will wrap the single instance as a group
        /// and call validation for the <see cref="IGroupValidatable"/>.</remarks>
        internal static ResultReport ValidateOne(this IAssertion assertion, IScopedNode input, ValidationSettings vc, ValidationState state) =>
            assertion switch
            {
                IValidatable validatable => validatable.Validate(input, vc, state),
                _ => ResultReport.SUCCESS
            };

        /// <summary>
        /// Validates a single instance element using an assertion.
        /// </summary>
        /// <remarks>If the assertion is an <see cref="IValidatable"/>, this will simply invoke the
        /// corresponding method on the validator. If not, it will wrap the single instance as a group
        /// and call validation for the <see cref="IGroupValidatable"/>.</remarks>
        internal static ValueTask<ResultReport> ValidateOneAsync(this IAssertion assertion, IScopedNode input, ValidationSettings vc, ValidationState state, CancellationToken cancellationToken) =>
            assertion switch
            {
                IValidatable validatable => validatable.ValidateAsync(input, vc, state, cancellationToken),
                _ => new(ResultReport.SUCCESS)
            };

        /// <summary>
        /// Tests whether the given assertion has a given fixed (meaning: not depending on the input) outcome.
        /// See <see cref="IFixedResult"/>.
        /// </summary>
        public static bool IsAlways(this IAssertion assertion, ValidationResult result) =>
            assertion is IFixedResult fr && fr.FixedResult == result;
    }
}