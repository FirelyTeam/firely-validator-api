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
        /// <summary>
        /// Lower bound for the cardinality. If not set, there is no lower bound.
        /// </summary>
        [DataMember]
        public int? Min { get; private set; }

        /// <summary>
        /// Upper bound for the cardinality. If not set, there is no upper bound.
        /// </summary>
        [DataMember]
        public int? Max { get; private set; }

        /// <summary>
        /// Defines an assertion with the given minimum and maximum cardinalities.
        /// </summary>
        /// <remarks>If neither <paramref name="min"/> nor <paramref name="max"/> is given,
        /// the validation will always return success.</remarks>
        public CardinalityValidator(int? min = null, int? max = null)
        {
            if (min.HasValue && min.Value < 0)
                throw new IncorrectElementDefinitionException("Lower cardinality cannot be lower than 0.");
            if (max.HasValue && max.Value < 0)
                throw new IncorrectElementDefinitionException("Upper cardinality cannot be lower than 0.");
            if (min.HasValue && max.HasValue && min > max)
                throw new IncorrectElementDefinitionException("Upper cardinality must be higher than the lower cardinality.");

            Min = min;
            Max = max;
        }

        /// <summary>
        /// Defines an assertion with the given minimum and maximum cardinalities, were the
        /// optional maximum is either a number or an asterix (for no maximum).
        /// </summary>
        /// <param name="min">Any integer or null.</param>
        /// <param name="max">Should be null or "*" for no maximum, or a positive number otherwise.
        /// </param>
        public static CardinalityValidator FromMinMax(int? min, string? max)
        {
            int? intMax = max switch
            {
                null => null,
                "*" => null,
                string other when int.TryParse(other, out var maximum) => maximum,
                _ => throw new IncorrectElementDefinitionException("Upper cardinality shall be a positive number or '*'.")
            };

            return new CardinalityValidator(min, intMax);
        }

        /// <inheritdoc />
        public ResultAssertion Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext _, ValidationState __)
        {
            var count = input.Count();

            var result = !inRange(count) ?
                ResultAssertion.FromEvidence(new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, groupLocation,
                $"Instance count is {count}, which is not within the specified cardinality of {CardinalityDisplay}"))
                : ResultAssertion.SUCCESS;

            return result;
        }

        private bool inRange(int x) => (!Min.HasValue || x >= Min.Value) && (!Max.HasValue || x <= Max.Value);

        private string CardinalityDisplay => $"{Min?.ToString() ?? "<-"}..{Max?.ToString() ?? "*"}";

        /// <inheritdoc />
        public JToken ToJson() => new JProperty("cardinality", CardinalityDisplay);
    }
}
