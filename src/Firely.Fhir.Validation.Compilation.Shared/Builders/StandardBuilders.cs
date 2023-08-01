using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    internal class StandardBuilders : ICompilerExtension
    {
        private readonly ICompilerExtension[] _compilerExtensions;

        public StandardBuilders(IAsyncResourceResolver source)
        {
            _compilerExtensions = new ICompilerExtension[] {
                    new MaxLengthBuilder(),
                    new FixedBuilder(),
                    new PatternBuilder(),
                    new MinValueBuilder(),
                    new MaxValueBuilder(),
                    new FhirPathBuilder(),
                    new CardinalityBuilder(),
                    new RegexBuilder(),
                    new BindingBuilder(),
                    new ContentReferenceBuilder(),
                    new TypeReferenceBuilder(source),
                    new CanonicalBuilder(),
                };
        }

        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
            => _compilerExtensions.SelectMany(ce => ce.Build(nav, conversionMode));
    }
}