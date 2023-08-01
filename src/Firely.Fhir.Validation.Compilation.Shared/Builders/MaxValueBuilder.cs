using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    internal class MaxValueBuilder : ICompilerExtension
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.MaxValue is not null)
                yield return new MinMaxValueValidator(def.MaxValue.ToTypedElement(ModelInspector.Base), MinMaxValueValidator.ValidationMode.MaxValue);
        }
    }
}
