/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element (converted to a string) matches a given regular expression.
    /// </summary>
    [DataContract]
    public class RegExAssertion : SimpleAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string Pattern { get; private set; }
#else
        [DataMember]
        public string Pattern { get; private set; }
#endif

        private readonly Regex _regex;

        public RegExAssertion(string pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            _regex = new Regex($"^{pattern}$", RegexOptions.Compiled);
        }

        public override string Key => "regex";

        public override object Value => Pattern;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var value = toStringRepresentation(input);
            var success = _regex.Match(value).Success;

            return !success
                ? Task.FromResult(Assertions.EMPTY + ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, input.Location, $"Value '{value}' does not match regex '{Pattern}'")))
                : Task.FromResult(Assertions.SUCCESS);
        }

        private static string? toStringRepresentation(ITypedElement vp)
        {
            return vp == null || vp.Value == null ?
                null :
                PrimitiveTypeConverter.ConvertTo<string>(vp.Value);
        }

    }
}
