/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
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
    public class CardinalityAssertion : IGroupValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int? Min { get; private set; }

        [DataMember(Order = 1)]
        public string? Max { get; private set; }

        [DataMember(Order = 2)]
        public string? Location { get; private set; }
#else
        [DataMember]
        public int? Min { get; private set; }

        [DataMember]
        public string? Max { get; private set; }

        [DataMember]
        public string? Location { get; private set; }
#endif

        private readonly int _maxAsInteger;

        public CardinalityAssertion(int? min, string? max, string? location = null)
        {
            Location = location;
            if (min.HasValue && min.Value < 0)
                throw new IncorrectElementDefinitionException("min cannot be lower than 0");

            int maximum = 0;
            if (max is not null && ((!int.TryParse(max, out maximum) && max != "*") || maximum < 0))
                throw new IncorrectElementDefinitionException("max SHALL be a positive number or '*'");

            Min = min;
            _maxAsInteger = maximum;
            Max = max;
        }

        public Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var assertions = new Assertions(new Trace("[CardinalityAssertion] Validating"));

            var count = input.Count();
            if (!inRange(count))
            {

                assertions += new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, Location, $"Instance count for '{Location}' is { count }, which is not within the specified cardinality of {CardinalityDisplay}");
            }

            return Task.FromResult(assertions.AddResultAssertion());
        }

        private bool inRange(int x) => (!Min.HasValue || x >= Min.Value) && (Max == "*" || Max is null || x <= _maxAsInteger);

        private string CardinalityDisplay => $"{Min?.ToString() ?? "<-"}..{Max ?? "->"}";

        public JToken ToJson()
        {
            return new JProperty("cardinality", CardinalityDisplay);
        }
    }
}
