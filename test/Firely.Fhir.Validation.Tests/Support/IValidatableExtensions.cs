using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Helper class to make it easier to test <see cref="IValidatable"/> and <see cref="IGroupValidatable"/> implementations with
    /// the interface <see cref="ITypedElement"/> instead of <see cref="IScopedNode"/>.
    /// </summary>
    internal static class IValidatableExtensions
    {
        /// <inheritdoc cref="IValidatable.Validate(IScopedNode, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IValidatable validatable, ITypedElement input, ValidationSettings vc, ValidationState state)
            => validatable.Validate(input.AsScopedNode(), vc, state);

        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{IScopedNode}, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IGroupValidatable validatable, IEnumerable<ITypedElement> input, ValidationSettings vc, ValidationState state)
            => validatable.Validate(input.Select(i => i.AsScopedNode()), vc, state);
    }
}