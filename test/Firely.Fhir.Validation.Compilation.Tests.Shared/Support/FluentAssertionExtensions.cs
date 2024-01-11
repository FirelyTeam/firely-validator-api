/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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

