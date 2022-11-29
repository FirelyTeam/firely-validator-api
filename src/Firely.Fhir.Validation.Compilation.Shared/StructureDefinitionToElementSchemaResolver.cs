/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System;
using System.Collections.Concurrent;

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
        public static IElementSchemaResolver CreatedCached(SchemaConverterSettings settings) =>
            new CachedElementSchemaResolver(
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(settings),
                    new SystemNamespaceElementSchemaResolver()
                    ));

        /// <inheritdoc cref="CreatedCached(SchemaConverterSettings)"/>
        public static IElementSchemaResolver CreatedCached(SchemaConverterSettings settings, ConcurrentDictionary<Canonical, ElementSchema?> cache) =>
            new CachedElementSchemaResolver(
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(settings),
                    new SystemNamespaceElementSchemaResolver()),
                cache);

        /// <summary>
        /// Creates an <see cref="IElementSchemaResolver"/> that includes support for resolving types from
        /// the System/CQL namespace.
        /// </summary>
        public static IElementSchemaResolver Create(SchemaConverterSettings settings) =>
                new MultiElementSchemaResolver(
                    new StructureDefinitionToElementSchemaResolver(settings),
                    new SystemNamespaceElementSchemaResolver());

        /// <summary>
        /// The <see cref="SchemaConverterSettings"/> used when converting a resolved StructureDefinition to an ElementSchema.
        /// </summary>
        public SchemaConverterSettings Settings { get; private set; }

        internal StructureDefinitionToElementSchemaResolver(SchemaConverterSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Builds a schema directly from an <see cref="ElementDefinitionNavigator" /> without fetching
        /// it from the underlying resolver.
        /// </summary>
        public IValidatable GetSchema(ElementDefinitionNavigator nav) => new SchemaConverter(Settings).Convert(nav);

        /// <summary>
        /// Use the <see cref="Settings"/> to retrieve a StructureDefinition and turn it into an
        /// <see cref="ElementSchema"/>.
        /// </summary>
        /// <returns>The schema, or <c>null</c> if the schema uri could not be resolved as a
        /// StructureDefinition canonical.</returns>
        public ElementSchema? GetSchema(Canonical schemaUri) =>
            new SchemaConverter(Settings).TryConvert(schemaUri, out var schema) is true ? schema : null;
    }
}