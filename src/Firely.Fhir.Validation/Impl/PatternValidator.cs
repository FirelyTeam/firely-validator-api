/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
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
        /// <summary>
        /// The pattern the instance will be validated against.
        /// </summary>
        [DataMember]
        public ITypedElement PatternValue { get; private set; }

        /// <summary>
        /// Initializes a new PatternValidator given a pattern.
        /// </summary>
        public PatternValidator(ITypedElement patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        /// <summary>
        /// Initializes a new PatternValidator given a pattern using a (primitive) .NET value.
        /// </summary>
        /// <remarks>The .NET primitive will be turned into a <see cref="ITypedElement"/> based
        /// pattern using <see cref="ElementNode.ForPrimitive(object)"/>, so this constructor
        /// supports any conversion done there.</remarks>
        public PatternValidator(object patternPrimitive) : this(ElementNode.ForPrimitive(patternPrimitive)) { }

        /// <inheritdoc/>
        public ResultReport Validate(ITypedElement input, ValidationContext _, ValidationState s)
        {
            var result = !input.Matches(PatternValue)
                ? new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, $"Value does not match pattern '{PatternValue.ToJson()}")
                    .AsResult(input, s)
                : ResultReport.SUCCESS;

            return result;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"pattern[{PatternValue.InstanceType}]", PatternValue.ToPropValue());
    }
}
