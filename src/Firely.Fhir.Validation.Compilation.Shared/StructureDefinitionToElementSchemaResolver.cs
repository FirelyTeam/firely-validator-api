/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// This is an implementation of <see cref="IElementSchemaResolver"/> that takes a 
    /// FHIR StructureDefintion as input and converts it so an <see cref="ElementSchema"/>.
    /// </summary>
    /// <remarks>For this to work, it is assumed that the schema URI maps one-to-one to the
    /// canonical url of the StructureDefinition. This class takes an <see cref="IAsyncResourceResolver"/>
    /// as a dependency, and will simply forward the given schema uri to this resolver.
    /// 
    /// Also, since schema generation is expensive, this resolver will cache the results
    /// and return the already-converted schema for the same uri the next time <see cref="GetSchema(Canonical)"/>" is called.
    /// </remarks>
    internal class StructureDefinitionToElementSchemaResolver : IElementSchemaResolver // internal?
    {
        private readonly SchemaBuilder _schemaBuilder;

        /// <summary>
        /// Creates an <see cref="IElementSchemaResolver" /> that includes for resolving types from
        /// the System/CQL namespace and that uses caching to optimize performance.
        /// </summary>
        public static IElementSchemaResolver CreatedCached(IAsyncResourceResolver source, IEnumerable<ISchemaBuilder>? extraSchemaBuilders = null) =>
            new CachedElementSchemaResolver(Create(source, extraSchemaBuilders));

        /// <inheritdoc cref="CreatedCached(IAsyncResourceResolver, IEnumerable{ISchemaBuilder}?)"/>"
        public static IElementSchemaResolver CreatedCached(IAsyncResourceResolver source, ConcurrentDictionary<Canonical, ElementSchema?> cache) =>
            new CachedElementSchemaResolver(Create(source), cache);

        /// <summary>
        /// Creates an <see cref="IElementSchemaResolver"/> that includes support for resolving types from
        /// the System/CQL namespace.
        /// </summary>
        public static IElementSchemaResolver Create(IAsyncResourceResolver source, IEnumerable<ISchemaBuilder>? extraSchemaBuilders = null)
        {
            var builders = new List<ISchemaBuilder>();

            // is StandardBuilder not included in extraSchemaBuilders?
            if (extraSchemaBuilders?.FirstOrDefault(b => b is StandardBuilders) is null)
                builders.Add(new StandardBuilders(source));

            builders.AddRange(extraSchemaBuilders ?? Enumerable.Empty<ISchemaBuilder>());

            return new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(
                        new StructureDefinitionCorrectionsResolver(source), builders),
                    new SystemNamespaceElementSchemaResolver());
        }

        /// <summary>
        /// The <see cref="IAsyncResourceResolver"/> used as the source for resolving StructureDefinitions,
        /// as passed to the constructor.
        /// </summary>
        public IAsyncResourceResolver Source { get; private set; }

        /// <summary>
        /// Constructs the new resolver.
        /// </summary>
        /// <param name="source">The <see cref="IAsyncResourceResolver"/> to use to fetch a
        /// source StructureDefinition for a schema uri.</param>
        /// <param name="schemaBuilders"></param>
        internal StructureDefinitionToElementSchemaResolver(IAsyncResourceResolver source, IEnumerable<ISchemaBuilder>? schemaBuilders = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _schemaBuilder = new SchemaBuilder(Source, schemaBuilders ?? Enumerable.Empty<ISchemaBuilder>());
        }

        /// <summary>
        /// Constructs the new resolver with the standard schemabuilders.
        /// </summary>
        /// <param name="source">The <see cref="IAsyncResourceResolver"/> to use to fetch a
        /// source StructureDefinition for a schema uri.</param>
        internal StructureDefinitionToElementSchemaResolver(IAsyncResourceResolver source) :
            this(source, new[] { new StandardBuilders(source) })
        {
            // Nothing
        }

        /// <summary>
        /// Builds a schema directly from an <see cref="ElementDefinitionNavigator" /> without fetching
        /// it from the underlying <see cref="Source"/>. />
        /// </summary>
        public IValidatable GetSchema(ElementDefinitionNavigator nav) => SchemaBuilderExtensions.BuildSchema(_schemaBuilder, nav)!;

        /// <summary>
        /// Use the <see cref="Source"/> to retrieve a StructureDefinition and turn it into an
        /// <see cref="ElementSchema"/>.
        /// </summary>
        /// <param name="schemaUri">The canonical url of the StructureDefinition.</param>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved as a
        /// StructureDefinition canonical.</returns>
        public ElementSchema? GetSchema(Canonical schemaUri)
        {
            try
            {
                return TaskHelper.Await(() => Source.FindStructureDefinitionAsync((string)schemaUri)) is StructureDefinition sd
                    ? _schemaBuilder.BuildSchema(sd)
                    : null;
            }
            catch (Exception e)
            {
                throw new SchemaResolutionFailedException(
                    $"Encountered an error while loading schema '{schemaUri}': {e.Message}",
                    schemaUri, e);
            }
        }
    }
}