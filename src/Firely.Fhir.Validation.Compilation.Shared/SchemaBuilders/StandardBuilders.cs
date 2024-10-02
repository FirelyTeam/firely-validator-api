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
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The standard schema builder.
    /// </summary>
    /// <remarks>
    /// Create a standard set of schema builders for validating FHIR resources and datatypes
    /// </remarks>
    /// <param name="source"></param>
    public class StandardBuilders(IAsyncResourceResolver source) : ISchemaBuilder
    {
        private readonly ISchemaBuilder[] _schemaBuilders = new ISchemaBuilder[] {
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
                    new FhirStringBuilder(),
                    new FhirUriBuilder()
                };

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
            => _schemaBuilders.SelectMany(ce => ce.Build(nav, conversionMode));
        async Task<IAssertion[]> ISchemaBuilder.BuildAsync(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode, CancellationToken cancellationToken)
        {
            var assertions = new List<IAssertion>();
            for (int i = 0; i < _schemaBuilders.Length; i++)
            {
                assertions.AddRange(await _schemaBuilders[i].BuildAsync(nav, conversionMode, cancellationToken));
            }
            
            return assertions.ToArray();
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete