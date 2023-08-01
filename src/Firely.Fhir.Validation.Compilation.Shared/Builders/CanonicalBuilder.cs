using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Add an extra validator rule for the FHIR datatype canonical
    /// </summary>
    internal class CanonicalBuilder : ICompilerExtension
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (nav.Current.ElementId is "canonical.value") yield return new CanonicalValidator();
        }

    }
}