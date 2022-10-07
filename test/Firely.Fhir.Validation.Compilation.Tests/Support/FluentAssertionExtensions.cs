/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using FluentAssertions.Equivalency;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    /// <summary>
    /// Own defined extension methods for the FluentAssertions framework
    /// </summary>
    public static class FluentAssertionExtensions
    {
        /// <summary>
        /// Handle BeEquivalentTo differently for <see cref="Canonical"/>
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static EquivalencyAssertionOptions<TProperty> UsingCanonicalCompare<TProperty>(this EquivalencyAssertionOptions<TProperty> options)
            => options.Using<Canonical>(ctx => ctx.Subject.Original.Should().BeEquivalentTo(ctx.Expectation.Original)).WhenTypeIs<Canonical>();
    }
}

