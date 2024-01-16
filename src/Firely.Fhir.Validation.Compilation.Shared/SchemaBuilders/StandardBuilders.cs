/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
                    new CanonicalBuilder()
                };
        }

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
            => _schemaBuilders.SelectMany(ce => ce.Build(nav, conversionMode));
    }
}