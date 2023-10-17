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
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element is exactly the same as a given fixed value.
    /// </summary>
    [DataContract]
    public class FixedValidator : IValidatable
    {
        /// <summary>
        /// The fixed value to compare an instance against.
        /// </summary>
        [DataMember]
        public ITypedElement FixedValue { get; private set; }

        /// <summary>
        /// Initializes a new FixedValidator given the fixed value.
        /// </summary>
        public FixedValidator(ITypedElement fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
        }

        /// <summary>
        /// Initializes a new FixedValidator given a (primitive) .NET value.
        /// </summary>
        /// <remarks>The .NET primitive will be turned into a <see cref="ITypedElement"/> based
        /// fixed value using <see cref="ElementNode.ForPrimitive(object)"/>, so this constructor
        /// supports any conversion done there.</remarks>
        public FixedValidator(object fixedValue) : this(ElementNode.ForPrimitive(fixedValue)) { }

        /// <inheritdoc />
        public ResultReport Validate(ITypedElement input, ValidationContext _, ValidationState s)
        {
            if (!input.IsExactlyEqualTo(FixedValue, ignoreOrder: true))
            {
                return new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE,
                        $"Value '{displayValue(input)}' is not exactly equal to fixed value '{displayValue(FixedValue)}'")
                        .AsResult(s);
            }

            return ResultReport.SUCCESS;

            static string displayValue(ITypedElement te) =>
                te.Children().Any() ? te.ToJson() : te.Value.ToString();
        }

        /// <inheritdoc />
        public JToken ToJson() => new JProperty($"Fixed[{FixedValue.InstanceType}]", FixedValue.ToPropValue());
    }
}
