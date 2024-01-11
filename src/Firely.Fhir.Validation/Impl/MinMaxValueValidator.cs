/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the maximum (or minimum) value for an element.
    /// </summary>
    [DataContract]
    internal class MinMaxValueValidator : IValidatable
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

            static bool isOrderedType(Any value) => value is ICqlOrderable;
        }

        /// <inheritdoc cref="MinMaxValueValidator(ITypedElement, ValidationMode)"/>
        public MinMaxValueValidator(long limit, ValidationMode minMaxType) : this(ElementNode.ForPrimitive(limit), minMaxType) { }

        /// <inheritdoc/>
        public ResultReport Validate(IScopedNode input, ValidationSettings _, ValidationState s)
        {
            if (!Any.TryConvert(input.Value, out var instanceValue))
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE,
                            $"Value '{input.Value}' cannot be compared with {Limit.Value})").AsResult(s);
            }

            try
            {
                if ((instanceValue is ICqlOrderable ce ? ce.CompareTo(_minMaxAnyValue) : -1) == _comparisonOutcome)
                {
                    return new IssueAssertion(_comparisonIssue, $"Value '{input.Value}' is {_comparisonLabel} {Limit.Value})")
                        .AsResult(s);
                }
            }
            catch (ArgumentException)
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE,
                        $"Value '{input.Value}' cannot be compared with {Limit.Value})")
                    .AsResult(s);
            }

            return ResultReport.SUCCESS;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"{_minMaxLabel}[{Limit.InstanceType}]", Limit.ToPropValue());
    }
}
