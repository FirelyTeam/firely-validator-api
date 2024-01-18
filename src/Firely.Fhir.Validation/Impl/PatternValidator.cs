/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Serialization;
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
    internal class PatternValidator : IValidatable
    {
        /// <summary>
        /// The pattern value to compare against.
        /// </summary>
        [DataMember]
        public ITypedElement PatternValue { get; }

        /// <summary>
        /// Initializes a new PatternValidator given a pattern using a (primitive) .NET value.
        /// </summary>
        public PatternValidator(ITypedElement patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        /// <inheritdoc/>
        public ResultReport Validate(IScopedNode input, ValidationSettings _, ValidationState s)
        {
            var result = input.Matches(PatternValue)
              ? ResultReport.SUCCESS
              : new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, $"Value '{input.ToScopedNode().ToJson()}' does not match pattern '{PatternValue.ToJson()}'")  // TODO: add value to message
                  .AsResult(s);

            return result;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"pattern[{PatternValue.InstanceType}]", PatternValue.ToJson());
    }
}
