/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// This implementation of <see cref="IElementSchemaResolver"/> will resolve a single uri against multiple 
    /// child resolvers: the result of the first child resolver to return a non-null resolution will be used.
    /// </summary>
    internal class MultiElementSchemaResolver : IElementSchemaResolver
    {
        /// <summary>
        /// The set of child <see cref="IElementSchemaResolver"/> used as the source for resolving schemas,
        /// as passed to the constructor.
        /// </summary>
        public IReadOnlyCollection<IElementSchemaResolver> Sources { get; private set; }

        /// <summary>
        /// Constructs a caching resolver that uses a collection of child resolvers to resolve
        /// a uri to an <see cref="ElementSchema"/>.
        /// </summary>
        public MultiElementSchemaResolver(IEnumerable<IElementSchemaResolver> sources)
        {
            Sources = new List<IElementSchemaResolver>(sources) ?? throw new ArgumentNullException(nameof(sources));
        }

        /// <inheritdoc cref="MultiElementSchemaResolver(IEnumerable{IElementSchemaResolver})"/>
        public MultiElementSchemaResolver(params IElementSchemaResolver[] sources) : this((IEnumerable<IElementSchemaResolver>)sources)
        {
            // nothing
        }

        /// <summary>
        /// Consults the child resolvers in order (first in the list first), by calling their 
        /// <see cref="GetSchema(Canonical)"/> method.
        /// Will stop and return the result of the first resolver to return non-null.
        /// </summary>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved by any of the child resolvers.</returns>
        public ElementSchema? GetSchema(Canonical schemaUri)
        {
            // This won't work unless we use async enumerables - which are not yet available for all platfors.
            // return Sources.Select(s => s.GetSchema(schemaUri)).FirstOrDefault(res => res is not null);

            foreach (var resolver in Sources)
                if (resolver.GetSchema(schemaUri) is { } result) return result;

            return null;
        }
    }
}