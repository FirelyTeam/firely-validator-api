/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System;
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
            => assertion switch
            {
                IValidatable validatable => await validatable.Validate(input, vc, state).ConfigureAwait(false),
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(input, vc, state).ConfigureAwait(false),
                _ => Assertions.SUCCESS,
            };

        internal static async Task<Assertions> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc, ValidationState state)
           => await assertion.Validate(new[] { input }, vc, state).ConfigureAwait(false);

        internal static async Task<Assertions> Validate(this IValidatable assertion, IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
            => input.Any() ? await input.Select(ma => assertion.Validate(ma, vc, state)).AggregateAsync() : Assertions.EMPTY;

        internal static async Task<Assertions> Validate(Uri uri, IEnumerable<ITypedElement> input, ValidationContext vc, ValidationState state)
        {
            var schema = await vc.ElementSchemaResolver!.GetSchema(uri).ConfigureAwait(false);
            return schema is null
                ? new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                null, $"A schema cannot be found for uri {uri.OriginalString}.")))
                : await schema.Validate(input, vc, state).ConfigureAwait(false);
        }

        internal static async Task<Assertions> Validate(Uri uri, ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var schema = await vc.ElementSchemaResolver!.GetSchema(uri).ConfigureAwait(false);
            return schema is null
                ? new Assertions(new ResultAssertion(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_REFERENCE_NOT_RESOLVABLE,
                null, $"A schema cannot be found for uri {uri.OriginalString}.")))
                : await schema.Validate(input, vc, state).ConfigureAwait(false);
        }

    }
}