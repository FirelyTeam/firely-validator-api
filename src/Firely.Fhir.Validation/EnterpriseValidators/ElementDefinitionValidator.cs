using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR ElementDefinition
    /// </summary>    
    [DataContract]
    public class ElementDefinitionValidator : IValidatable
    {
        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("elementDefinition", new JObject());

        /// <inheritdoc/>
        public ResultReport Validate(IScopedNode input, ValidationContext vc, ValidationState state)
        {
            var evidence = new List<ResultReport>();

            //this can be expanded with other validate functionality
            evidence.AddRange(validateTypeCompatibilityOfValues(input, state));

            return ResultReport.FromEvidence(evidence);
        }

        /// <summary>
        /// Validates if the type of fixed, pattern, examples, minValue, and maxValue correspond to the type(s) defined in ElementDefinition.type
        /// </summary>
        /// <param name="input"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static IEnumerable<ResultReport> validateTypeCompatibilityOfValues(IScopedNode input, ValidationState state)
        {
            var typeNames = getTypeNames(input);


            if (typeNames.Any())
            {
                var fixedType = input.Children("fixed").Select(f => f.InstanceType);
                var patternType = input.Children("pattern").Select(f => f.InstanceType);
                var exampleTypes = input.Children("example")
                                        .SelectMany(e => e.Children("value")
                                            .Select(c => c.InstanceType));
                var minValueType = input.Children("minValue").Select(m => m.InstanceType);
                var maxValueType = input.Children("maxValue").Select(m => m.InstanceType);

                var fixedEvidence = validateType(fixedType, "fixed", typeNames, input, state);
                var patternEvidence = validateType(patternType, "pattern", typeNames, input, state);
                var examplesEvidence = validateType(exampleTypes, "example.value", typeNames, input, state);
                var minValueEvidence = validateType(minValueType, "minValue", typeNames, input, state);
                var maxValueEvidence = validateType(maxValueType, "maxValue", typeNames, input, state);

                return fixedEvidence
                    .Concat(patternEvidence)
                    .Concat(examplesEvidence)
                    .Concat(minValueEvidence)
                    .Concat(maxValueEvidence);
            }

            return Enumerable.Empty<ResultReport>();
        }

        private static IEnumerable<string> getTypeNames(IScopedNode input)
        {
            var typeComponents = input.Children("type");
            return typeComponents.SelectMany(t =>
                                        t.Children("code")
                                        .TakeWhile(c => c.Value?.ToString() is not null))
                                        .Select(c => c.Value.ToString());
        }

        private static IEnumerable<ResultReport> validateType(IEnumerable<string> valueTypes, string propertyName, IEnumerable<string> typeNames, IScopedNode input, ValidationState state)
        {
            return valueTypes.Where(t => !typeNames.Contains(t))
                                      .Select(t => new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, $"Type of the {propertyName} property '{t}' doesn't match with the type(s) of the element '{string.Join(',', typeNames)}'").AsResult(state));

        }
    }
}

#nullable restore
