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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element matches a given pattern value.
    /// </summary>
    /// <remarks>The rules of whether an instance matches a pattern are laid down in 
    /// the description of <c>ElementDefinition</c>'s 
    /// <a href="http://hl7.org/fhir/elementdefinition-definitions.html#ElementDefinition.pattern_x_</remarks>">pattern element</a>
    /// in the FHIR specification.</remarks>
    [DataContract]
    public class PatternValidator : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public ITypedElement PatternValue { get; private set; }
#else
        [DataMember]
        public ITypedElement PatternValue { get; private set; }
#endif

        public PatternValidator(ITypedElement patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        public PatternValidator(object patternPrimitive) : this(ElementNode.ForPrimitive(patternPrimitive)) { }

        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            var result = !input.Matches(PatternValue)
                ? ResultAssertion.FromEvidence(
                        new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, input.Location, $"Value does not match pattern '{PatternValue.ToJson()}"))
                : ResultAssertion.SUCCESS;

            return Task.FromResult(result);
        }

        public JToken ToJson() => new JProperty($"pattern[{PatternValue.InstanceType}]", PatternValue.ToPropValue());
    }
}
