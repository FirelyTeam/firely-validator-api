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

            var issues = new List<IssueAssertion>();

            //this can be expanded with other validate functionality
            issues.AddRange(validateTypeCompatibilityOfValues(input));

            return issues.Any()
                  ? new ResultReport(ValidationResult.Failure, issues)
                  : new ResultReport(ValidationResult.Success);
        }

        /// <summary>
        /// Validates if the type of fixed, pattern, examples, minValue, and maxValue correspond to the type(s) defined in ElementDefinition.type
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static List<IssueAssertion> validateTypeCompatibilityOfValues(ITypedElement input)
        {
            var fixedType = input.Children("fixed").FirstOrDefault()?.InstanceType;
            var patternType = input.Children("pattern").FirstOrDefault()?.InstanceType;
            var exampleTypes = input.Children("example")?
                                    .Select(e => e.Children("value")
                                        .FirstOrDefault()?.InstanceType)
                                    .Where(e => e is not null);

            var minValueType = input.Children("minValue").FirstOrDefault()?.InstanceType;
            var maxValueType = input.Children("maxValue").FirstOrDefault()?.InstanceType;

            var typeNames = getTypeNames(input);

            var issues = new List<IssueAssertion>();

            if (fixedType is not null)
            {
                validateType(fixedType, "fixed", typeNames, issues);
            }

            if (patternType is not null)
            {
                validateType(patternType, "pattern", typeNames, issues);
            }

            if (exampleTypes?.Any() == true)
            {
                foreach (var exampleType in exampleTypes)
                {
                    validateType(exampleType!, "example.value", typeNames, issues);
                }
            }

            if (minValueType is not null)
            {
                validateType(minValueType, "minValue", typeNames, issues);
            }

            if (maxValueType is not null)
            {
                validateType(maxValueType, "maxValue", typeNames, issues);
            }

            return issues;
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

        private static void validateType(string valueType, string propertyName, IEnumerable<string> typeNames, List<IssueAssertion> issues)
        {
            if (!typeNames.Contains(valueType))
            {
                issues.Add(new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, $"Type of the {propertyName} property '{valueType}' doesn't match with the type(s) of the element '{string.Join(',', typeNames)}'"));
            }
        }

    }
}
