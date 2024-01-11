/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
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
    internal class FixedValidator : IValidatable
    {
        private readonly JToken _fixedJToken;

        /// <summary>
        /// The fixed value to compare against.
        /// </summary>
        [DataMember]
        public DataType FixedValue { get; }

        /// <summary>
        /// Initializes a new FixedValidator given a (primitive) .NET value.
        /// </summary>
        public FixedValidator(DataType fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
            _fixedJToken = FixedValue.ToJToken();
        }

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationSettings _, ValidationState s)
        {
            var fixedValue = FixedValue.ToScopedNode();
            if (!input.IsExactlyEqualTo(fixedValue, ignoreOrder: true))
            {
                return new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE,
                        $"Value '{displayValue(input)}' is not exactly equal to fixed value '{displayJToken(_fixedJToken)}'")
                        .AsResult(s);
            }

            return ResultReport.SUCCESS;

            static string displayValue(IScopedNode te) => te.Children().Any() ? ToJson(te) : te.Value.ToString()!;

            static string displayJToken(JToken jToken) =>
                jToken is JValue val
                ? val.ToString()
                : jToken.ToString(Newtonsoft.Json.Formatting.None);

            static string ToJson(IScopedNode instance)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var node = instance.AsTypedElement();
#pragma warning restore CS0618 // Type or member is obsolete
                return node.ToJObject().ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        /// <inheritdoc />
        public JToken ToJson() => new JProperty($"Fixed[{FixedValue.TypeName}]", _fixedJToken);
    }
}
