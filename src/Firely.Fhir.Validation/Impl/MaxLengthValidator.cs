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
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a maximum length on an element that contains a string value. 
    /// </summary>
    [DataContract]
    public class MaxLengthValidator : BasicValidator
    {
        /// <summary>
        /// The maximum length the string in the instance should be.
        /// </summary>
        [DataMember]
        public int MaximumLength { get; private set; }

        /// <summary>
        /// Initializes a new MaxLengthValidator given a maximum length.
        /// </summary>
        /// <param name="maximumLength"></param>
        public MaxLengthValidator(int maximumLength)
        {
            if (maximumLength <= 0)
                throw new IncorrectElementDefinitionException($"{nameof(maximumLength)}: Must be a positive number");

            MaximumLength = maximumLength;
        }

        /// <inheritdoc />
        protected override string Key => "maxLength";

        /// <inheritdoc />
        protected override object Value => MaximumLength;

        /// <inheritdoc />
        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState s)
        {
            if (input == null) throw Error.ArgumentNull(nameof(input));

            if (Any.Convert(input.Value) is String serializedValue)
            {
                return serializedValue.Value.Length > MaximumLength
                    ? new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG,
                        $"Value '{serializedValue}' is too long (maximum length is {MaximumLength}").AsResult(s)
                    : ResultReport.SUCCESS;
            }
            else
            {
                var result = vc.TraceResult(() =>
                        new TraceAssertion(s.Location.InstanceLocation.ToString(),
                        $"Validation of a max length for a non-string (type is {input.InstanceType} here) always succeeds."));
                return result;
            }
        }
    }
}