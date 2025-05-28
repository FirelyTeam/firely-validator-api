using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Helper class to make it easier to test <see cref="IValidatable"/> and <see cref="IGroupValidatable"/> implementations with
    /// the interface <see cref="PocoNode"/> instead of <see cref="PocoNode"/>.
    /// </summary>
    internal static class IValidatableExtensions
    {
        /// <inheritdoc cref="IValidatable.Validate(PocoNode, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IValidatable validatable, PocoNode input, ValidationSettings vc, ValidationState state)
        {
            ArgumentNullException.ThrowIfNull(input);
            return validatable.Validate(input, vc, state);
        }


        /// <inheritdoc cref="IGroupValidatable.Validate(IEnumerable{PocoNode}, ValidationSettings, ValidationState)"/>
        public static ResultReport Validate(this IGroupValidatable validatable, IEnumerable<PocoNode> input, ValidationSettings vc, ValidationState state)
        {
            ArgumentNullException.ThrowIfNull(input);
            return validatable.Validate(input.Select(i => i.ToPocoNode()), vc, state);
        }
    }
}