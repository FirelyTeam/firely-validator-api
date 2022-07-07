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

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the maximum (or minimum) value for an element.
    /// </summary>
    [DataContract]
    public class MinMaxValueValidator : IValidatable
    {
        /// <summary>
        /// Represents a mode op operation for the <see cref="MinMaxValueValidator"/>.
        /// </summary>
        public enum ValidationMode
        {
            /// <summary>
            /// The <see cref="Limit"/> is used as a minium value.
            /// </summary>
            MinValue,

            /// <summary>
            /// The <see cref="Limit"/> is used as a maximum value.
            /// </summary>
            MaxValue
        }

        /// <summary>
        /// The minimum or maximum limit value. Comparison is done using the CQL comparison semantics.
        /// (see https://cql.hl7.org/09-b-cqlreference.html#comparison-operators-4).
        /// </summary>
        [DataMember]
        public ITypedElement Limit { get; private set; }

        /// <summary>
        /// Whether this validator is enforcing a maximum or minimum value.
        /// </summary>
        [DataMember]
        public ValidationMode MinMaxType { get; private set; }

        private readonly string _minMaxLabel;
        private readonly Any _minMaxAnyValue;
        private readonly int _comparisonOutcome;
        private readonly string _comparisonLabel;
        private readonly Issue _comparisonIssue;

        /// <summary>
        /// Initializes a MinMaxValueValidator given a limit and the mode opf operation.
        /// </summary>
        public MinMaxValueValidator(ITypedElement limit, ValidationMode minMaxType)
        {
            Limit = limit ?? throw new ArgumentNullException(nameof(limit), $"{nameof(limit)} cannot be null");
            MinMaxType = minMaxType;

            if (Any.TryConvert(Limit.Value, out _minMaxAnyValue!) == false)
                throw new IncorrectElementDefinitionException($"Cannot convert the limit value ({Limit.Value}) to a comparable primitive.");

            _comparisonOutcome = MinMaxType == ValidationMode.MinValue ? -1 : 1;
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

        /// <inheritdoc cref="MinMaxValueValidator(ITypedElement, ValidationMode)"/>
        public MinMaxValueValidator(long limit, ValidationMode minMaxType) : this(ElementNode.ForPrimitive(limit), minMaxType) { }

        /// <inheritdoc/>
        public ResultAssertion Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            if (!Any.TryConvert(input.Value, out var instanceValue))
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, input.Location,
                            $"Value '{input.Value}' cannot be compared with {Limit.Value})").AsResult();
            }

            try
            {
                if ((instanceValue is ICqlOrderable ce ? ce.CompareTo(_minMaxAnyValue) : -1) == _comparisonOutcome)
                {
                    return new IssueAssertion(_comparisonIssue, input.Location, $"Value '{input.Value}' is {_comparisonLabel} {Limit.Value})").AsResult();
                }
            }
            catch (ArgumentException)
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE, input.Location,
                        $"Value '{input.Value}' cannot be compared with {Limit.Value})").AsResult();
            }

            return ResultAssertion.SUCCESS;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"{_minMaxLabel}[{Limit.InstanceType}]", Limit.ToPropValue());

        /// <summary>
        /// TODO Validation: this should be altered and moved to a more generic place, and should be more sophisticated
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool isOrderedType(Any value) => value is ICqlOrderable;
    }
}
