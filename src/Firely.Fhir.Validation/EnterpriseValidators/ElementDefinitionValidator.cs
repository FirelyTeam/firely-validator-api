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
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
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
        private static List<ResultReport> validateTypeCompatibilityOfValues(ITypedElement input, ValidationState state)
        {
            var typeNames = getTypeNames(input);
            var evidence = new List<ResultReport>();

            if (typeNames.Any())
            {
                var fixedType = input.Children("fixed").FirstOrDefault()?.InstanceType;
                var patternType = input.Children("pattern").FirstOrDefault()?.InstanceType;
                var exampleTypes = input.Children("example")?
                                        .Select(e => e.Children("value")
                                            .FirstOrDefault()?.InstanceType)
                                        .Where(e => e is not null);

                var minValueType = input.Children("minValue").FirstOrDefault()?.InstanceType;
                var maxValueType = input.Children("maxValue").FirstOrDefault()?.InstanceType;

                if (fixedType is not null)
                {
                    validateType(fixedType, "fixed", typeNames, evidence, input, state);
                }

                if (patternType is not null)
                {
                    validateType(patternType, "pattern", typeNames, evidence, input, state);
                }

                if (exampleTypes?.Any() == true)
                {
                    foreach (var exampleType in exampleTypes)
                    {
                        validateType(exampleType!, "example.value", typeNames, evidence, input, state);
                    }
                }

                if (minValueType is not null)
                {
                    validateType(minValueType, "minValue", typeNames, evidence, input, state);
                }

                if (maxValueType is not null)
                {
                    validateType(maxValueType, "maxValue", typeNames, evidence, input, state);
                }
            }


            return evidence;
        }

        private static IEnumerable<string> getTypeNames(ITypedElement input)
        {
            var typesComponents = input.Children("type");
            if (typesComponents is not null && typesComponents.Any())
            {
                var typeNames = typesComponents.Select(t => t.Children("code")
                                .FirstOrDefault()?
                                .Value?.ToString());

                //remove null values.
                if (typeNames != null && typeNames.Any())
                {
                    return typeNames!
                           .Where(n => n is not null)
                           .Select(n => n!);
                }
            }

            return Enumerable.Empty<string>();
        }

        private static void validateType(string valueType, string propertyName, IEnumerable<string> typeNames, List<ResultReport> evidence, ITypedElement input, ValidationState state)
        {
            if (!typeNames.Contains(valueType))
            {
                var issue = new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, $"Type of the {propertyName} property '{valueType}' doesn't match with the type(s) of the element '{string.Join(',', typeNames)}'");
                evidence.Add(issue.AsResult(input, state));
            }
        }
    }
}
