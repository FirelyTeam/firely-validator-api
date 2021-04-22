using System;
using System.Linq;
using System.Text.Json;
using static System.Text.Json.JsonElement;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class JsonAsserter
    {
        public static void AssertJsonDocument(JsonDocument expected, JsonDocument actual, Action<string> logger)
            => assertJsonElement(expected.RootElement, actual.RootElement, logger, location: "root");

        private static void assertJsonElement(JsonElement expected, JsonElement actual, Action<string> logger, string location = "root")
        {
            if (actual.ValueKind != expected.ValueKind)
            {
                logger($"Expected ValueKind {expected.ValueKind} is not the same as actual {actual.ValueKind} at location '{location}'");
            }

            switch (actual.ValueKind)
            {
                case JsonValueKind.Object:
                    assertJsonObject(expected.EnumerateObject(), actual.EnumerateObject(), logger, location);
                    break;
                case JsonValueKind.Array:
                    assertJsonArray(expected.EnumerateArray(), actual.EnumerateArray(), logger, location);
                    break;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (actual.GetRawText() != expected.GetRawText())
                    {
                        logger($"Expected value {expected.GetRawText()} is not the same as actual {actual.GetRawText()} at location '{location}'");
                    }
                    break;
                case JsonValueKind.Null:
                    break;
                case JsonValueKind.Undefined:
                    break;
                default:
                    break;
            }
        }

        private static void assertJsonObject(ObjectEnumerator expected, ObjectEnumerator actual, Action<string> logger, string location)
        {
            // same properties actual <-> expected
            var intersect = (from exp in expected
                             join act in actual
                             on exp.Name equals act.Name
                             select new
                             {
                                 Exptected = exp,
                                 Actual = act
                             }).ToList();

            var names = intersect.Select(s => s.Exptected.Name);

            var moreInExpected = expected.Where(a => !names.Contains(a.Name));

            foreach (var item in moreInExpected)
            {
                logger($"{item.Name} was expected but not present at location '{location}'");
            }

            foreach (var item in intersect)
            {
                assertJsonElement(item.Exptected.Value, item.Actual.Value, logger, $"{location}.{item.Exptected.Name}");
            }
        }

        private static void assertJsonArray(ArrayEnumerator expected, ArrayEnumerator actual, Action<string> logger, string location)
        {
            if (actual.Count() != expected.Count())
            {
                logger($"Expected count of array {expected.Count()} is not the same as actual {actual.Count()} at location '{location}'");
            }

            var i = 0;
            while (expected.MoveNext() && actual.MoveNext())
            {
                assertJsonElement(expected.Current, actual.Current, logger, $"{location}[{i}]");
                i++;
            }

        }
    }
}
