/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Utility;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a validation result.
    /// </summary>
    [DataContract]
    public class ResultAssertion : BasicValidator, IFixedResult
    {
        /// <summary>
        /// Will validate to a success assertion without evidence.
        /// </summary>
        public static readonly ResultAssertion SUCCESS = new(ValidationResult.Success);

        /// <summary>
        /// Will validate to an undecided assertion without evidence.
        /// </summary>
        public static readonly ResultAssertion UNDECIDED = new(ValidationResult.Undecided);

        /// <summary>
        /// Will validate to a failure assertion without evidence.
        /// </summary>
        public static readonly ResultAssertion FAILURE = new(ValidationResult.Failure);

        /// <summary>
        /// The result of the validation.
        /// </summary>
        [DataMember]
        public ValidationResult Result => _fixedReport.Result;

        private readonly ResultReport _fixedReport;

        // Serialization constructor
        private ResultAssertion(ValidationResult result)
        {
            _fixedReport = getResultAssertion(result);
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

        ValidationResult IFixedResult.FixedResult => Result;

        /// <inheritdoc/>
        public override ResultReport Validate(ROD input, ValidationContext vc, ValidationState state) => _fixedReport;

        /// <inheritdoc/>
        public ResultReport AsResult() => _fixedReport;
    }
}