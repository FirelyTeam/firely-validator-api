/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public enum MinMax
    {
        [EnumLiteral("MinValue"), Description("Minimum Value")]
        MinValue,
        [EnumLiteral("MaxValue"), Description("Maximum Value")]
        MaxValue
    }

    /// <summary>
    /// Asserts the maximum (or minimum) value for an element.
    /// </summary>
    [DataContract]
    public class MinMaxValueValidator : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public ITypedElement Limit { get; private set; }

        [DataMember(Order = 1)]
        public MinMax MinMaxType { get; private set; }
#else
        [DataMember]
        public ITypedElement Limit { get; private set; }

        [DataMember]
        public MinMax MinMaxType { get; private set; }
#endif

        private readonly string _minMaxLabel;
        private readonly Any _minMaxAnyValue;
        private readonly int _comparisonOutcome;
        private readonly string _comparisonLabel;
        private readonly Issue _comparisonIssue;

        public MinMaxValueValidator(ITypedElement limit, MinMax minMaxType)
        {
            Limit = limit ?? throw new ArgumentNullException(nameof(limit), $"{nameof(limit)} cannot be null");
            MinMaxType = minMaxType;

            if (Any.TryConvert(Limit.Value, out _minMaxAnyValue!) == false)
                throw new IncorrectElementDefinitionException($"Cannot convert the limit value ({Limit.Value}) to a comparable primitive.");

            _comparisonOutcome = MinMaxType == MinMax.MinValue ? -1 : 1;
            _comparisonLabel = _comparisonOutcome == -1 ? "smaller than" :
                                    _comparisonOutcome == 0 ? "equal to" :
                                        "larger than";
            _comparisonIssue = _comparisonOutcome == -1 ? Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL :
                                           Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE;

            _minMaxLabel = $"{MinMaxType.GetLiteral().Uncapitalize()}";

            // Min/max are only defined for ordered types
            if (!isOrderedType(_minMaxAnyValue))
                throw new IncorrectElementDefinitionException($"{Limit.Name} was given in ElementDefinition, but type '{Limit.InstanceType}' is not an ordered type");
        }

        public MinMaxValueValidator(long limit, MinMax minMaxType) : this(ElementNode.ForPrimitive(limit), minMaxType) { }

        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            if (!Any.TryConvert(input.Value, out var instanceValue))
            {
                return Task.FromResult(
                       ResultAssertion.FromEvidence(
                            new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, input.Location,
                            $"Value '{input.Value}' cannot be compared with {Limit.Value})")));
            }

            try
            {
                if ((instanceValue is ICqlOrderable ce ? ce.CompareTo(_minMaxAnyValue) : -1) == _comparisonOutcome)
                {
                    return Task.FromResult(
                        ResultAssertion.FromEvidence(
                                new IssueAssertion(_comparisonIssue, input.Location, $"Value '{input.Value}' is {_comparisonLabel} {Limit.Value})")));
                }
            }
            catch (ArgumentException)
            {
                return Task.FromResult(
                    ResultAssertion.FromEvidence(
                        new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, input.Location,
                        $"Value '{input.Value}' cannot be compared with {Limit.Value})")));
            }

            return Task.FromResult(ResultAssertion.SUCCESS);
        }

        public JToken ToJson() => new JProperty($"{_minMaxLabel}[{Limit.InstanceType}]", Limit.ToPropValue());

        /// <summary>
        /// TODO Validation: this should be altered and moved to a more generic place, and should be more sophisticated
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool isOrderedType(Any value) => value is ICqlOrderable;
    }
}
