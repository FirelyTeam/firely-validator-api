/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion expressed using FhirPath.
    /// </summary>
    [DataContract]
    internal abstract class InvariantValidator : IValidatable
    {
        /// <summary>
        /// The shorthand code identifying the invariant, as defined in the StructureDefinition.
        /// </summary>
        public abstract string Key { get; }

        /// <summary>
        /// The human-readable description of the invariant (for error messages).
        /// </summary>
        public abstract string? HumanDescription { get; }

        /// <summary>
        /// Whether failure to meet the invariant is considered an error or not.
        /// </summary>
        /// <remarks>When the severity is anything else than <see cref="IssueSeverity.Error"/>, the
        /// <see cref="ResultReport"/> returned on failure to meet the invariant will be a 
        /// <see cref="ValidationResult.Success"/>,
        /// and have an <see cref="IssueAssertion"/> evidence with severity level <see cref="IssueSeverity.Warning"/>.
        /// </remarks>
        public abstract IssueSeverity? Severity { get; }

        /// <summary>
        /// Whether the invariant describes a "best practice" rather than a real invariant.
        /// </summary>
        /// <remarks>When this constraint is a "best practice", the outcome of validation is determined
        /// by the value of <see cref="ValidationSettings.ConstraintBestPractices"/>.</remarks>
        public abstract bool BestPractice { get; }

        ///<inheritdoc cref="IJsonSerializable.ToJson"/>
        public abstract JToken ToJson();

        /// <summary>
        /// Implements the logic for running the invariant.
        /// </summary>
        protected abstract (bool, ResultReport?) RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState s);

        /// <inheritdoc />
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState s)
        {
            var (success, directAssertion) = RunInvariant(input, vc, s);

            if (directAssertion is not null) return directAssertion;

            if (!success)
            {
                var sev = BestPractice
                    ? vc.ConstraintBestPractices switch
                    {
                        ValidateBestPracticesSeverity.Error => (IssueSeverity?)IssueSeverity.Error,
                        ValidateBestPracticesSeverity.Warning => (IssueSeverity?)IssueSeverity.Warning,
                        _ => throw new InvalidOperationException($"Unknown value for enum {nameof(ValidateBestPracticesSeverity)}."),
                    }
                    : Severity;

                return new IssueAssertion(sev == IssueSeverity.Error ?
                        Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT :
                        Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT,
                        $"Instance failed constraint {getDescription()}").AsResult(s);
            }
            else
                return ResultReport.SUCCESS;

            string getDescription() => Key +
                (!string.IsNullOrEmpty(HumanDescription) ? $" \"{HumanDescription}\"" : null);

        }
    }
}