using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Firely.Fhir.Validation.Tests.Support
{
    [TestClass]
    public class ScopedNodeJsonTests
    {
        [TestMethod]
        [DynamicData(nameof(AdditionData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetTestDisplayNames))]
        public void ToJTokenTest(DataType dataType, string expectedResult)
        {
            dataType.ToJToken().ToString().Should().Be(expectedResult);
        }

        public static IEnumerable<object[]> AdditionData() => GetDataTypeExamples().Select(x => new object[] { x.Item1, x.Item2 });

        public static string GetTestDisplayNames(MethodInfo methodInfo, object[] values) =>
            $"{methodInfo.Name}({(values[0] is DataType dt ? dt.TypeName : values[0].GetType().Name)}, '{values[1]}')";

        public static IEnumerable<(DataType, string)> GetDataTypeExamples()
        {
            yield return (new CodeableConcept() { Coding = [new("System", "code"), new("Sys2", "code2")] }, """
                {
                  "coding": [
                    {
                      "system": "System",
                      "code": "code"
                    },
                    {
                      "system": "Sys2",
                      "code": "code2"
                    }
                  ]
                }
                """);
            yield return (new Coding("System", "Code"), """
                {
                  "system": "System",
                  "code": "Code"
                }
                """);

            yield return (new Money() { Currency = Money.Currencies.EUR, Value = 3 }, """
                {
                  "value": 3.0,
                  "currency": "EUR"
                }
                """);
            yield return (new FhirBoolean(true), "True");
            yield return (new Code("male"), "male");
            yield return (new FhirDateTime(2023, 12, 05), "2023-12-05");
            yield return (new FhirDecimal(5), "5");
            yield return (new FhirString("a string"), "a string");
        }
    }
}
