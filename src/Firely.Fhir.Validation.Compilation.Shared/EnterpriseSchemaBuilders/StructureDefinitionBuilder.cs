using Hl7.Fhir.Specification.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Firely.Fhir.Validation.Compilation.Shared.EnterpriseSchemaBuilders
{
    internal class StructureDefinitionBuilder : ISchemaBuilder
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            return new StructureDefinitionBuilder();
        }
    }
}
