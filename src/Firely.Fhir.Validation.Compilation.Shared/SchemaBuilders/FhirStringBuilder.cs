using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class FhirStringBuilder : ISchemaBuilder
    {

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (nav.Current.ElementId is "string.value") yield return new FhirStringValidator();
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
