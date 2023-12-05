﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element matches a given pattern value.
    /// </summary>
    /// <remarks>The rules of whether an instance matches a pattern are laid down in 
    /// the description of <c>ElementDefinition</c>'s 
    /// <a href="http://hl7.org/fhir/elementdefinition-definitions.html#ElementDefinition.pattern_x_">pattern element</a>
    /// in the FHIR specification.</remarks>
    [DataContract]
    public class PatternValidator : IValidatable
    {
        private readonly JToken _patternJToken;

        /// <summary>
        /// The pattern value to compare against.
        /// </summary>
        [DataMember]
        public DataType PatternValue { get; }

        /// <summary>
        /// Initializes a new PatternValidator given a pattern using a (primitive) .NET value.
        /// </summary>
        public PatternValidator(DataType patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
            _patternJToken = PatternValue.ToJToken();
        }

        /// <inheritdoc/>
        public ResultReport Validate(IScopedNode input, ValidationContext _, ValidationState s)
        {
            var patternValue = PatternValue.ToScopedNode();
            var result = input.Matches(patternValue)
              ? ResultReport.SUCCESS
              : new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, $"Value does not match pattern '{displayJToken(_patternJToken)}")
                  .AsResult(s);

            return result;

            static string displayJToken(JToken jToken) =>
                jToken is JValue val
                ? val.ToString()
                : jToken.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"pattern[{PatternValue.TypeName}]", _patternJToken);
    }
}
