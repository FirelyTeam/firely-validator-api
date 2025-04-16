using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Helper class to make it easier to test <see cref="IValidatable"/> and <see cref="IGroupValidatable"/> implementations with
    /// the interface <see cref="ITypedElement"/> instead of <see cref="ITypedElement"/>.
    /// </summary>
    internal static class IValidatableExtensions
    {
        /// <inheritdoc cref="IValidatable.Validate(ITypedElement, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IValidatable validatable, ITypedElement input, ValidationSettings vc, ValidationState state)
        {
            ArgumentNullException.ThrowIfNull(input);
            return validatable.Validate(input, vc, state);
        }


        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{ITypedElement}, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IGroupValidatable validatable, IEnumerable<ITypedElement> input, ValidationSettings vc, ValidationState state)
        {
            ArgumentNullException.ThrowIfNull(input);
            return validatable.Validate(input.Select(i => i.ToPocoNode()), vc, state);
        }
    }
}