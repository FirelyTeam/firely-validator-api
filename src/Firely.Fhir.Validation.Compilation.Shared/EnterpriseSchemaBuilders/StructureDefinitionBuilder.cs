using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="ElementDefinitionValidator"/>.
    /// </summary>
    internal class StructureDefinitionBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (nav.Current.Path == "StructureDefinition")
            {
                yield return new StructureDefinitionValidator();
            };
        }
    }
}
