using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Shared.EnterpriseSchemaBuilders
{
    /// <summary>
    /// 
    /// </summary>
    public class ElementDefinitionBuilder : ISchemaBuilder
    {
        /// <summary>
        /// The schema builder for the <see cref="ElementDefinitionValidator"/>.
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="conversionMode"></param>
        /// <returns></returns>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            if (nav.Current.ElementId is "ElementDefinition") yield return new ElementDefinitionValidator();
        }
    }
}
