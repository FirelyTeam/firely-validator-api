﻿/* 
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
        /// <summary>
        /// The fixed value to compare against.
        /// </summary>
        [DataMember]
        public ITypedElement FixedValue { get; }

        /// <summary>
        /// Initializes a new FixedValidator given a (primitive) .NET value.
        /// </summary>
        public FixedValidator(ITypedElement fixedValue)
        {
            FixedValue = fixedValue ?? throw new ArgumentNullException(nameof(fixedValue));
        }

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationSettings _, ValidationState s)
        {
            if (!input.IsExactlyEqualTo(FixedValue, ignoreOrder: true))
            {
                return new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE,
                        $"Value '{displayValue(input.ToScopedNode())}' is not exactly equal to fixed value '{displayValue(FixedValue)}'")
                        .AsResult(s);
            }

            return ResultReport.SUCCESS;
            
            static string displayValue(ITypedElement te) =>
                te.Children().Any() ? te.ToJson() : te.Value.ToString()!;
        }

        /// <inheritdoc />
        public JToken ToJson() => new JProperty($"Fixed[{FixedValue.InstanceType}]", FixedValue.ToPropValue());
    }
    
    
    
}
