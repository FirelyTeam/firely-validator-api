using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface representing a group of <see cref="Assertions" /> with a unique identifier. />
    /// </summary>
    public interface IElementSchema : IAssertion, IGroupValidatable
    {
        Uri Id { get; }

        IEnumerable<IAssertion> Members { get; }
    }

    public static class IElementSchemaExtensions
    {
        public static bool IsEmpty(this IElementSchema elementSchema)
            => !elementSchema.Members.Any();

        public static IElementSchema With(this IElementSchema elementSchema, IEnumerable<IAssertion> additional) =>
             new ElementSchema(elementSchema.Id, elementSchema.Members.Union(additional));

        public static IElementSchema With(this IElementSchema elementSchema, params IAssertion[] additional)
            => elementSchema.With(additional.AsEnumerable());

        public static void LogSchema(this IElementSchema elementSchema)
        {
            var json = elementSchema.ToJson();

            Debug.WriteLine($"Elementschema: {elementSchema.Id}\n{json}");
        }
    }
}
