using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public static class ElementSchemaExtensions
    {
        public static bool IsEmpty(this ElementSchema elementSchema)
            => !elementSchema.Members.Any();

        public static ElementSchema With(this ElementSchema elementSchema, IEnumerable<IAssertion> additional) =>
             new(elementSchema.Id, elementSchema.Members.Union(additional));

        public static ElementSchema With(this ElementSchema elementSchema, params IAssertion[] additional)
            => elementSchema.With(additional.AsEnumerable());

        public static void LogSchema(this ElementSchema elementSchema)
        {
            var json = elementSchema.ToJson();

            Debug.WriteLine($"Elementschema: {elementSchema.Id}\n{json}");
        }
    }
}
