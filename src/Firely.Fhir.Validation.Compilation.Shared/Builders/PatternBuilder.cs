using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>This constraint is not part of an element refering to a backbone type (see eld-5).</remarks>
    internal class PatternBuilder : ICompilerExtension
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            if (def.Pattern is not null)
                yield return new PatternValidator(def.Pattern.ToTypedElement(ModelInspector.Base));
        }
    }
}
