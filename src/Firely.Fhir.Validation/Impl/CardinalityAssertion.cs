﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class CardinalityAssertion : IGroupValidatable
    {
        private readonly int? _min;
        private readonly int _max;
        private readonly string _maxInText;
        private readonly string? _location;


        public CardinalityAssertion(int? min, string max, string? location = null)
        {
            _location = location;
            if (min.HasValue && min.Value < 0)
                throw new IncorrectElementDefinitionException("min cannot be lower than 0");

            int maximum = 0;
            if (max is not null && ((!int.TryParse(max, out maximum) && max != "*") || maximum < 0))
                throw new IncorrectElementDefinitionException("max SHALL be a positive number or '*'");

            _min = min;
            _max = maximum;
            _maxInText = max;
        }

        public Task<Assertions> Validate(IEnumerable<ITypedElement> input, ValidationContext vc)
        {
            var assertions = Assertions.Empty + new Trace("[CardinalityAssertion] Validating");

            var count = input.Count();
            if (!inRange(count))
            {

                assertions += new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, _location, $"Instance count for '{_location}' is { count }, which is not within the specified cardinality of {CardinalityDisplay}");
            }

            return Task.FromResult(assertions.AddResultAssertion());
        }

        private bool inRange(int x)
        {
            if (_min.HasValue && x < _min.Value)
                return false;

            if (_maxInText == "*" || _maxInText == null)
                return true;

            return x <= _max;
        }

        private string CardinalityDisplay => $"{_min?.ToString() ?? "<-"}..{_maxInText ?? "->"}";

        public JToken ToJson()
        {
            return new JProperty("cardinality", CardinalityDisplay);
        }
    }
}
