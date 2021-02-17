using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public static class ValidationExtensions
    {
        public static async Task<Assertions> Validate(this IAssertion assertion, IEnumerable<ITypedElement> input, ValidationContext vc)
            => assertion switch
            {
                IValidatable validatable => await validatable.Validate(input, vc).ConfigureAwait(false),
                IGroupValidatable groupvalidatable => await groupvalidatable.Validate(input, vc).ConfigureAwait(false),
                _ => Assertions.SUCCESS,
            };

        public static async Task<Assertions> Validate(this IAssertion assertion, ITypedElement input, ValidationContext vc)
           => await assertion.Validate(new[] { input }, vc).ConfigureAwait(false);

        public static async Task<Assertions> Validate(this IValidatable assertion, IEnumerable<ITypedElement> input, ValidationContext vc)
            => input.Any() ? await assertion.Validate(input.Single(), vc).ConfigureAwait(false) : Assertions.EMPTY;
        // to protect that IValidatables are executed with null

        public static async Task<Assertions> Validate(Uri uri, IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var schema = await vc.ElementSchemaResolver!.GetSchema(uri).ConfigureAwait(false);
            if (schema is null) throw new ArgumentException($"A schema cannot be found for uri {uri.OriginalString}.", nameof(uri));
            return await schema.Validate(input, vc).ConfigureAwait(false);
        }

        public static async Task<Assertions> Validate(Uri uri, ITypedElement input, ValidationContext vc)
        {
            var schema = await vc.ElementSchemaResolver!.GetSchema(uri).ConfigureAwait(false);
            if (schema is null) throw new ArgumentException($"A schema cannot be found for uri {uri.OriginalString}.", nameof(uri));
            return await schema.Validate(input, vc).ConfigureAwait(false);
        }
    }
}