/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Utility;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a maximum length on an element that contains a string value. 
    /// </summary>
    [DataContract]
    public class MaxLength : SimpleAssertion
    {
        [DataMember(Order = 0)]
        public int MaximumLength { get; private set; }

        public MaxLength(int maxLength)
        {
            if (maxLength <= 0)
                throw new IncorrectElementDefinitionException($"{nameof(maxLength)}: Must be a positive number");

            MaximumLength = maxLength;
        }

        public override string Key => "maxLength";

        public override object Value => MaximumLength;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            if (input == null) throw Error.ArgumentNull(nameof(input));

            var result = Assertions.EMPTY + this;

            if (Any.Convert(input.Value) is String serializedValue)
            {
                result += new Trace($"Maxlength validation with value '{serializedValue}'");

                if (serializedValue.Value.Length > MaximumLength)
                {
                    return Task.FromResult(result + ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, input.Location, $"Value '{serializedValue}' is too long (maximum length is {MaximumLength}")));
                }
            }
            else return Task.FromResult(Assertions.UNDECIDED);

            return Task.FromResult(result + Assertions.SUCCESS);
        }

    }
}