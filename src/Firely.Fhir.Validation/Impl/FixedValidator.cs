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
    /// Asserts that the value of an element is exactly the same as a given fixed value.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class FixedValidator : IValidatable
    {
        /// <summary>
        /// The fixed value to compare against.
        /// </summary>
        [DataMember]
        public PocoNode FixedValue { get; }

        /// <summary>
        /// Initializes a new FixedValidator given a (primitive) .NET value.
        /// </summary>
        public FixedValidator(PocoNode fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings _, ValidationState s)
        {
            if (!input.IsExactlyEqualTo(FixedValue, ignoreOrder: true))
            {
                return new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE,
                        $"Value '{displayValue(input)}' is not exactly equal to fixed value '{displayValue(FixedValue)}'")
                        .AsResult(s, input, nameof(FixedValidator));
            }

            return ResultReport.SUCCESS;

            static string displayValue(ITypedElement pn) =>
                pn.Children().Any() ? pn.ToJson() : pn.Value!.ToString()!;
        }

        /// <inheritdoc />
        public JToken ToJson() => new JProperty($"fixed[{FixedValue.Poco.TypeName}]", FixedValue.ToPropValue());
    }



}
