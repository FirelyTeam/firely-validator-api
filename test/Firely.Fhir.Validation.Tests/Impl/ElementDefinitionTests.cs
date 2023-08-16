using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ElementDefinitionTests
    {
        [TestMethod]
        public void ElementDefinitionFixedTypeTest()
        {
            var elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "boolean"
                    },
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirBoolean(false)
            };

        }
    }
}
