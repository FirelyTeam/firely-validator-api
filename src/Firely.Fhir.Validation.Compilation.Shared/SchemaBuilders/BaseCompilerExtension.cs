using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    public abstract class BaseCompilerExtension : ISchemaBuilder
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav,
           ElementConversionMode? conversionMode = ElementConversionMode.Full) =>
            Build(nav.Current, nav.StructureDefinition, !nav.HasChildren, conversionMode);

        public abstract IEnumerable<IAssertion> Build(ElementDefinition def, StructureDefinition structureDefinition, bool isUnconstrainedElement = false, ElementConversionMode? conversionMode = ElementConversionMode.Full);

    }
}
