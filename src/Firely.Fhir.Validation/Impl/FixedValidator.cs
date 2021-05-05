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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element is exactly the same as a given fixed value.
    /// </summary>
    [DataContract]
    public class FixedValidator : IValidatable
    {
        [DataMember]
        public ITypedElement FixedValue { get; private set; }

        public FixedValidator(ITypedElement fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
        }

        public FixedValidator(object fixedValue) : this(ElementNode.ForPrimitive(fixedValue)) { }

        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            if (EqualityOperators.IsEqualTo(FixedValue, input) != true)
            {
                var result = ResultAssertion.FromEvidence(new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, input.Location,
                    $"Value '{displayValue(input)}' is not exactly equal to fixed value '{displayValue(FixedValue)}'"));

                return Task.FromResult(result);
            }

            return Task.FromResult(ResultAssertion.SUCCESS);

            //TODO: we need a better ToString() for ITypedElement
            static string displayValue(ITypedElement te) =>
                te.Children().Any() ? te.ToJson() : te.Value.ToString();
        }

        public JToken ToJson() => new JProperty($"Fixed[{FixedValue.InstanceType}]", FixedValue.ToPropValue());
    }
}
