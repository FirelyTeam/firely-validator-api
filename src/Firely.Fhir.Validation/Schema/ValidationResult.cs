/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The result of validation as determined by an assertion.
    /// </summary>
    public enum ValidationResult
    {
        /// <summary>
        /// The instance was valid according to the rules of the assertion.
        /// </summary>
        Success,

        /// <summary>
        /// The instance failed the rules of the assertion.
        /// </summary>
        Failure,

        /// <summary>
        /// The validity could not be asserted.
        /// </summary>
        Undecided
    }

    /// <summary>
    /// Extension methods that operate on the <see cref="ValidationResult"/> enumeration.
    /// </summary>
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// Determines the total result, given two partial results of a validation. This
        /// will propagate success, unless an undecided is encountered, which in its turn
        /// is overriden by a failure.
        /// </summary>
        /// <remarks><see cref="ValidationResult.Success"/> is the monoidal identity element.</remarks>
        public static ValidationResult Combine(this ValidationResult a, ValidationResult b) =>
            (a, b) switch
            {
                (_, ValidationResult.Failure) => ValidationResult.Failure,
                (ValidationResult.Failure, _) => ValidationResult.Failure,
                (ValidationResult.Undecided, _) => ValidationResult.Undecided,
                (ValidationResult.Success, var other) => other,
                _ => throw new NotSupportedException($"Enum values have been added to {nameof(ValidationResult)} " +
                    $"and {nameof(ValidationResultExtensions.Combine)}() should be adapted accordingly.")
            };
    }
}