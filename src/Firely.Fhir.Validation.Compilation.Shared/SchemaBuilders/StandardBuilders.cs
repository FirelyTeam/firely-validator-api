/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The standard schema builder.
    /// </summary>
    internal class StandardBuilders : ISchemaBuilder
    {
        private readonly ISchemaBuilder[] _schemaBuilders;

        public StandardBuilders(IAsyncResourceResolver source)
        {
            _schemaBuilders = new ISchemaBuilder[] {
                    new MaxLengthBuilder(),
                    new FixedBuilder(),
                    new PatternBuilder(),
                    new MinValueBuilder(),
                    new MaxValueBuilder(),
                    new BindingBuilder(),
                    new FhirPathBuilder(),
                    new CardinalityBuilder(),
                    new RegexBuilder(),
                    new ContentReferenceBuilder(),
                    new TypeReferenceBuilder(source),
                    new CanonicalBuilder(),
                    new ElementDefinitionBuilder()
                };
        }

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
            => _schemaBuilders.SelectMany(ce => ce.Build(nav, conversionMode));
    }
}