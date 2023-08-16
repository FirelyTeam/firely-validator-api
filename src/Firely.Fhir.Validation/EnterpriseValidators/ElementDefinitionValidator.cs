using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR StructureDefinition.
    /// </summary>    
    [DataContract]
    public class ElementDefinitionValidator : IValidatable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public JToken ToJson() => new JProperty("elementDefinition", new JObject());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="vc"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var fixedType = input.Children("fixed").FirstOrDefault()?.InstanceType;
            var patternType = input.Children("pattern").FirstOrDefault()?.InstanceType;
            var exampleTypes = input.Children("example")?.Select(e => e.Children("value").FirstOrDefault()?.InstanceType);
            var types = input.Children("type")?.Select(t => t.Children("code").FirstOrDefault()?.Value.ToString());

            var issues = new List<IssueAssertion>();

            if (fixedType != null)
            {
                if (types.Contains(fixedType))
                {
                    issues.Add(new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, "Type of the fixed value doesn't match with the type(s) of the element"));
                }
            }

            if (patternType != null)
            {
                if (types.Contains(patternType))
                {
                    issues.Add(new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, "Type of the pattern value doesn't match with the type(s) of the element"));
                }
            }

            if (exampleTypes?.Any() == true)
            {
                foreach (var exampleType in exampleTypes)
                {
                    issues.Add(new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, "Type of the example value doesn't match with the type(s) of the element"));
                }
            }

            return issues.Any()
                  ? new ResultReport(ValidationResult.Failure, issues)
                  : new ResultReport(ValidationResult.Success);
        }
    }
}
