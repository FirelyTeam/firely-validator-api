using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation;

#pragma warning disable CS0618 // Type or member is obsolete
internal class ExtensionContextBuilder : ISchemaBuilder
{
    public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode)
    {
        if (nav is { Path: "Extension", StructureDefinition.Type: "Extension" } && CommonExtensionContextComponent.TryCreate(nav, out var trc))
        {
            yield return new ExtensionContextValidator(trc.Contexts, trc.Invariants);
        }
    }
}
