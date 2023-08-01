using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    public interface ICompilerExtension
    {
        IEnumerable<IAssertion> Build(
           ElementDefinitionNavigator nav,
           ElementConversionMode? conversionMode = ElementConversionMode.Full);
    }
}