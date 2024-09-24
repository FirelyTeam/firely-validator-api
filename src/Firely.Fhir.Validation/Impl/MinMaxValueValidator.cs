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
using Hl7.FhirPath.Functions;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the maximum (or minimum) value for an element.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
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

            if (limit.InstanceType == "Quantity") //Quantity is the only non primitive that can be used as min/max value;
            {

                var quantity = limit.ParseQuantity()?.ToQuantity()!; // first parse to a Hl7.Model Qunatity, which we convert to a Hl7.Fhir.ElementModel.Types Quantity
                if (quantity is not null)
                {
                    _minMaxAnyValue = quantity!;
                }

                else
                    throw new IncorrectElementDefinitionException($"Cannot convert the limit value ({limit.Value}) to a quantity for comparison.");
            }
            else
            {
                if (Any.TryConvert(Limit.Value, out _minMaxAnyValue!) == false)
                    throw new IncorrectElementDefinitionException($"Cannot convert the limit value ({Limit.Value}) to a comparable primitive.");

                // Min/max are only defined for ordered types
                if (!isOrderedType(_minMaxAnyValue))
                    throw new IncorrectElementDefinitionException($"{Limit.Name} was given in ElementDefinition, but type '{Limit.InstanceType}' is not an ordered type");

                static bool isOrderedType(Any value) => value is ICqlOrderable;
            }

            _comparisonOutcome = MinMaxType == ValidationMode.MinValue ? -1 : 1;
            _comparisonLabel = _comparisonOutcome == -1 ? "smaller than" :
                                    _comparisonOutcome == 0 ? "equal to" :
                                        "larger than";
            _comparisonIssue = _comparisonOutcome == -1 ? Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL :
                                            Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE;
            _minMaxLabel = $"{MinMaxType.GetLiteral().Uncapitalize()}";

        }

        /// <inheritdoc cref="MinMaxValueValidator(ITypedElement, ValidationMode)"/>
        public MinMaxValueValidator(long limit, ValidationMode minMaxType) : this(ElementNode.ForPrimitive(limit), minMaxType) { }

        /// <inheritdoc/>
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings _, ValidationState s)
        {
            Any instanceValue;
            if (input.InstanceType == "Quantity")
            {
                var quantity = input.ParseQuantity()?.ToQuantity()!; // first parse to a Hl7.Model Qunatity, which we convert to a Hl7.Fhir.ElementModel.Types Quantity
                if (quantity is not null)
                {
                    instanceValue = quantity;
                }
                else
                {
                    return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE,
                          $"Value '{input.Value ?? input}' cannot be compared with {_minMaxAnyValue})").AsResult(s);
                }
            }
            else if (!Any.TryConvert(input.Value, out instanceValue!))
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE,
                            $"Value '{input.Value}' cannot be compared with {_minMaxAnyValue})").AsResult(s);
            }

            try
            {
                var (lt, gt) = (EqualityOperators.Compare(instanceValue, _minMaxAnyValue, "<"), EqualityOperators.Compare(instanceValue, _minMaxAnyValue, ">"));
                var intResult = (lt, gt) switch
                {
                    (true, _) => -1,
                    (false, true) => 1,
                    _ => 0
                };

                if (intResult == _comparisonOutcome)
                {
                    return new IssueAssertion(_comparisonIssue, $"Value '{instanceValue}' is {_comparisonLabel} {_minMaxAnyValue})")
                        .AsResult(s);
                }
            }
            catch (ArgumentException)
            {
                return new IssueAssertion(Issue.CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE,
                        $"Value '{instanceValue}' cannot be compared with {_minMaxAnyValue})")
                    .AsResult(s);
            }

            return ResultReport.SUCCESS;
        }

        /// <inheritdoc/>
        public JToken ToJson() => new JProperty($"{_minMaxLabel}[{Limit.InstanceType}]", Limit.ToPropValue());
    }
}
