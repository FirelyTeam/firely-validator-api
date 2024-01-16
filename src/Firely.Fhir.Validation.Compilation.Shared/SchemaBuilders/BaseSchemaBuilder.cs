/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// A helper class for defining a schema block. Instead of implementing the <see cref="ISchemaBuilder"/>,
    /// you can directly implement the <see cref="Build(ElementDefinition, StructureDefinition, bool, ElementConversionMode?)"/>
    /// method using the <see cref="ElementDefinition"/> parameter instead of the <see cref="ElementDefinitionNavigator"/>.
    /// </summary>
    internal abstract class BaseSchemaBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav,
           ElementConversionMode? conversionMode = ElementConversionMode.Full) =>
            Build(nav.Current, nav.StructureDefinition, !nav.HasChildren, conversionMode);

        /// <summary>
        /// Builds a schema block.
        /// </summary>
        /// <param name="def">The ElementDefinition for which a schema block needs to be constructed.</param>
        /// <param name="structureDefinition">The <see cref="StructureDefinition"/> associated with the <see cref="ElementDefinition"/> </param>
        /// <param name="isUnconstrainedElement"></param>
        /// <param name="conversionMode">The mode indicating the state we are in while constructing the schema block.</param>
        /// <returns></returns>
        public abstract IEnumerable<IAssertion> Build(
            ElementDefinition def,
            StructureDefinition structureDefinition,
            bool isUnconstrainedElement = false,
            ElementConversionMode? conversionMode = ElementConversionMode.Full);

    }
}
