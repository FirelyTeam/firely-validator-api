/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a validation result.
    /// </summary>
    [DataContract]
    public class ResultValidator : BasicValidator, IResultAssertion
    {
        /// <summary>
        /// Will validate to a success assertion without evidence.
        /// </summary>
        public static readonly ResultValidator SUCCESS = new(ResultReport.SUCCESS);

        /// <summary>
        /// Will validate to a success assertion without evidence.
        /// </summary>
        public static readonly ResultValidator UNDECIDED = new(ResultReport.UNDECIDED);

        /// <summary>
        /// Will validate to a success assertion without evidence.
        /// </summary>
        public static readonly ResultValidator FAILURE = new(ResultReport.FAILURE);

        /// <summary>
        /// The result of the validation.
        /// </summary>
        [DataMember]
        public ValidationResult Result => _assertion.Result;

        private readonly ResultReport _assertion;

        private ResultValidator(ResultReport assertion) => _assertion = assertion;

        // Serialization constructor
#pragma warning disable IDE0051 // Remove unused private members
        private ResultValidator(ValidationResult result) : this(getResultAssertion(result))
#pragma warning restore IDE0051 // Remove unused private members
        {
            // nothing
        }

        private static ResultReport getResultAssertion(ValidationResult result) =>
            result switch
            {
                ValidationResult.Success => ResultReport.SUCCESS,
                ValidationResult.Undecided => ResultReport.UNDECIDED,
                ValidationResult.Failure => ResultReport.FAILURE,
                _ => throw new NotSupportedException("Unkown ValidationResult encountered.")
            };

        /// <inheritdoc/>
        protected override string Key => "raise";

        /// <inheritdoc/>
        protected override object Value => Result.GetLiteral();

        /// <inheritdoc/>
        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state) => _assertion;
    }
}