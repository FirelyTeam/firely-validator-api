/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Validation;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertions that states lower and upper bounds on the number of items in a group of elements.
    /// </summary>
    /// <remarks>A good example is putting bounds on a group of elements with the same name, which translates to placing
    /// a cardinality restriction on the number of repeats of an element. But it can also be used for the number
    /// of elements in a slice for example.
    /// </remarks>
    [DataContract]
    public class CardinalityValidator : IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int? Min { get; private set; }

        [DataMember(Order = 1)]
        public int? Max { get; private set; }
        
        [DataMember(Order = 2)]
        public string? Location { get; private set; }
#else
        [DataMember]
        public int? Min { get; private set; }

        [DataMember]
        public int? Max { get; private set; }

        [DataMember]
        public string? Location { get; private set; }
#endif

        /// <summary>
        /// Defines an assertion with the given minimum and maximum cardinalities.
        /// </summary>
        /// <remarks>If neither <paramref name="min"/> nor <paramref name="max"/> is given,
        /// the validation will always return success.</remarks>
        public CardinalityValidator(int? min = null, int? max = null, string? location = null)
        {
            if (min.HasValue && min.Value < 0)
                throw new IncorrectElementDefinitionException("Lower cardinality cannot be lower than 0.");
            if (max.HasValue && max.Value < 0)
                throw new IncorrectElementDefinitionException("Upper cardinality cannot be lower than 0.");
            if (min.HasValue && max.HasValue && min > max)
                throw new IncorrectElementDefinitionException("Upper cardinality must be higher than the lower cardinality.");

            Min = min;
            Max = max;
            Location = location;
        }

        /// <summary>
        /// Defines an assertion with the given minimum and maximum cardinalities, were the
        /// optional maximum is either a number or an asterix (for no maximum).
        /// </summary>
        /// <param name="max">Should be null or "*" for no maximum, or a positive number otherwise.
        /// </param>
        public static CardinalityValidator FromMinMax(int? min, string? max, string? location = null)
        {
            int? intMax = max switch
            {
                null => null,
                "*" => null,
                string other when int.TryParse(other, out var maximum) => maximum,
                _ => throw new IncorrectElementDefinitionException("Upper cardinality shall be a positive number or '*'.")
            };

            return new CardinalityValidator(min, intMax, location);
        }

        public Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext _, ValidationState __)
        {
            var assertions = new Assertions(new TraceAssertion("[CardinalityAssertion] Validating"));

            var count = input.Count();
            if (!inRange(count))
            {
                assertions += new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, Location, $"Instance count for '{Location}' is { count }, which is not within the specified cardinality of {CardinalityDisplay}");
            }

            return Task.FromResult(assertions.AddResultAssertion());
        }

        private bool inRange(int x) => (!Min.HasValue || x >= Min.Value) && (!Max.HasValue || x <= Max.Value);

        private string CardinalityDisplay => $"{Min?.ToString() ?? "<-"}..{Max?.ToString() ?? "*"}";

        public JToken ToJson() => new JProperty("cardinality", CardinalityDisplay);
    }
}
