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
        private IAsyncResourceResolver _source;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public ElementDefinitionBuilder(IAsyncResourceResolver source)
        {

            _source = source;
        }

        /// <summary>
        /// The schema builder for the <see cref="ElementDefinitionValidator"/>.
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="conversionMode"></param>
        /// <returns></returns>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var schemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(_source);
            var schema = schemaResolver.GetSchema("http://hl7.org/fhir/ElementDefintion");

            if (schema != null)
            {
                yield return schema;
            }
        }
    }
}
