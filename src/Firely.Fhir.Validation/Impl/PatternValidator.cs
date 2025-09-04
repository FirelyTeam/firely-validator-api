/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
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
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class PatternValidator : IValidatable
    {
        /// <summary>
        /// The pattern value to compare against.
        /// </summary>
        [DataMember]
        public PocoNode PatternValue { get; }

        /// <summary>
        /// Initializes a new PatternValidator given a pattern using a (primitive) .NET value.
        /// </summary>
        public PatternValidator(PocoNode patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        /// <inheritdoc/>
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings _, ValidationState s)
        {
            if (input.Matches(PatternValue))
                return ResultReport.SUCCESS;
                    
            var severity = OperationOutcome.IssueSeverity.Error;
            // element with no value and extension - might have a special meaning, so let's make it into warning instead
            if (input is PrimitiveNode && input.GetValue() is null && (input.Poco as IExtendable)?.HasExtensions() is true)
                severity = OperationOutcome.IssueSeverity.Warning;
                    
            return new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE.Code, $"Value '{displayValue(input)}' does not match pattern '{displayValue(PatternValue)}'", severity, OperationOutcome.IssueType.Invalid)
                  .AsResult(s, input, nameof(PatternValidator));

            static string displayValue(ITypedElement te) =>
              te.Children().Any() ? te.ToJson() : te.Value?.ToString()!;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"pattern[{PatternValue.Poco.TypeName}]", PatternValue.ToPropValue());
    }
}
