/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
    public class StructureDefinitionToElementSchemaResolver : IElementSchemaResolver // internal?
    {
        /// <summary>
        /// Creates an <see cref="IElementSchemaResolver" /> that includes for resolving types from
        /// the System/CQL namespace and that uses caching to optimize performance.
        /// </summary>
        public static IElementSchemaResolver CreatedCached(IAsyncResourceResolver source) =>
            new CachedElementSchemaResolver(
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(source),
                    new SystemNamespaceElementSchemaResolver()
                    ));

        /// <inheritdoc cref="CreatedCached(IAsyncResourceResolver)"/>
        public static IElementSchemaResolver CreatedCached(IAsyncResourceResolver source, ConcurrentDictionary<Canonical, ElementSchema?> cache) =>
            new CachedElementSchemaResolver(
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(source),
                    new SystemNamespaceElementSchemaResolver()),
                cache);

        /// <summary>
        /// Creates an <see cref="IElementSchemaResolver"/> that includes support for resolving types from
        /// the System/CQL namespace.
        /// </summary>
        public static IElementSchemaResolver Create(IAsyncResourceResolver source) =>
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(source),
                    new SystemNamespaceElementSchemaResolver());

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
        internal StructureDefinitionToElementSchemaResolver(IAsyncResourceResolver source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Builds a schema directly from an <see cref="ElementDefinitionNavigator" /> without fetching
        /// it from the underlying <see cref="Source"/>. />
        /// </summary>
        public ElementSchema GetSchema(ElementDefinitionNavigator nav) => new SchemaConverter(Source).Convert(nav);

        /// <summary>
        /// Use the <see cref="Source"/> to retrieve a StructureDefinition and turn it into an
        /// <see cref="ElementSchema"/>.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved as a
        /// StructureDefinition canonical.</returns>
        public async Task<ElementSchema?> GetSchema(Canonical schemaUri) =>
            await Source.FindStructureDefinitionAsync((string)schemaUri) is StructureDefinition sd
                ? new SchemaConverter(Source).Convert(sd)
                : null;
    }
}