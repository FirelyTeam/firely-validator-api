using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Shared.EnterpriseSchemaBuilders
{
    /// <summary>
    /// The schema builder for the <see cref="ElementDefinitionValidator"/>.
    /// </summary>
    public class ElementDefinitionBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            if (nav.Current.ElementId is "ElementDefinition") yield return new ElementDefinitionValidator();
        }
    }
}
