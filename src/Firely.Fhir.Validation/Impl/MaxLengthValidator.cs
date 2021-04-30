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
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a maximum length on an element that contains a string value. 
    /// </summary>
    [DataContract]
    public class MaxLengthValidator : BasicValidator
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int MaximumLength { get; private set; }
#else
        [DataMember]
        public int MaximumLength { get; private set; }
#endif

        public MaxLengthValidator(int maximumLength)
        {
            if (maximumLength <= 0)
                throw new IncorrectElementDefinitionException($"{nameof(maximumLength)}: Must be a positive number");

            MaximumLength = maximumLength;
        }

        public override string Key => "maxLength";

        public override object Value => MaximumLength;

        public override Task<ResultAssertion> Validate(ITypedElement input, ValidationContext vc, ValidationState _)
        {
            if (input == null) throw Error.ArgumentNull(nameof(input));

            if (Any.Convert(input.Value) is String serializedValue)
            {
                if (serializedValue.Value.Length > MaximumLength)
                {
                    var result = ResultAssertion.FromEvidence(
                            new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, input.Location, $"Value '{serializedValue}' is too long (maximum length is {MaximumLength}")
                        );
                    return Task.FromResult(result);
                }
                else
                    return Task.FromResult(ResultAssertion.SUCCESS);
            }
            else
            {
                var result = vc.TraceResult(() =>
                        new TraceAssertion(input.Location,
                        $"Validation of a max length for a non-string (type is {input.InstanceType} here) always succeeds."));
                return Task.FromResult(result);
            }
        }
    }
}