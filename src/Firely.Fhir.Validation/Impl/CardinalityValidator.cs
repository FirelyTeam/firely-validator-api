/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
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
    internal class CardinalityValidator : IGroupValidatable
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
        public ResultReport Validate(IEnumerable<IScopedNode> input, ValidationSettings _, ValidationState s)
        {
            var count = input.Count();
            return buildResult(count, s);
        }

        private ResultReport buildResult(int count, ValidationState s) => !inRange(count) ?
                        new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE,
                        $"Instance count is {count}, which is not within the specified cardinality of {CardinalityDisplay}").AsResult(s)
                        : ResultReport.SUCCESS;

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state) =>
            buildResult(1, state);

        private bool inRange(int x) => (!Min.HasValue || x >= Min.Value) && (!Max.HasValue || x <= Max.Value);

        private string CardinalityDisplay => $"{Min?.ToString() ?? "<-"}..{Max?.ToString() ?? "*"}";

        /// <inheritdoc />
        public JToken ToJson() => new JProperty("cardinality", CardinalityDisplay);
    }
}
