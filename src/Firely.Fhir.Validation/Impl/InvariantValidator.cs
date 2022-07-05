/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
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
    public abstract class InvariantValidator : IValidatable
    {
        /// <summary>
        /// The shorthand code identifying the invariant, as defined in the StructureDefinition.
        /// </summary>
        [DataMember]
        public string Key { get; private set; }

        /// <summary>
        /// The human-readable description of the invariant (for error messages).
        /// </summary>
        [DataMember]
        public string? HumanDescription { get; private set; }

        /// <summary>
        /// Whether failure to meet the invariant is considered an error or not.
        /// </summary>
        /// <remarks>When the severity is anything else than <see cref="IssueSeverity.Error"/>, the
        /// <see cref="ResultAssertion"/> returned on failure to meet the invariant will be a 
        /// <see cref="ValidationResult.Success"/>,
        /// and have an <see cref="IssueAssertion"/> evidence with severity level <see cref="IssueSeverity.Warning"/>.
        /// </remarks>
        [DataMember]
        public IssueSeverity? Severity { get; private set; }

        /// <summary>
        /// Whether the invariant describes a "best practice" rather than a real invariant.
        /// </summary>
        /// <remarks>When this constraint is a "best practice", the outcome of validation is determined
        /// by the value of <see cref="ValidationContext.ConstraintBestPractices"/>.</remarks>
        [DataMember]
        public bool BestPractice { get; private set; }

        /// <summary>
        /// Initializes an InvariantValidator instance with the given FhirPath expression and identifying key.
        /// </summary>
        public InvariantValidator(string key, string expression) : this(key, expression, severity: IssueSeverity.Error) { }

        /// <summary>
        /// Initializes a FhirPathValidator instance with the given FhirPath expression, identifying key and other
        /// properties.
        /// </summary>
        public InvariantValidator(string key, string? humanDescription, IssueSeverity? severity = IssueSeverity.Error, bool bestPractice = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            HumanDescription = humanDescription;
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
            BestPractice = bestPractice;
        }

        /// <inheritdoc />
        protected virtual JObject ListProps()
        {
            var props = new JObject(
                     new JProperty("key", Key),
                     new JProperty("severity", Severity?.GetLiteral()),
                     new JProperty("bestPractice", BestPractice)
                    );

            if (HumanDescription != null)
                props.Add(new JProperty("humanDescription", HumanDescription));

            return props;
        }

        ///<inheritdoc cref="IJsonSerializable.ToJson"/>
        public abstract JToken ToJson();

        /// <summary>
        /// Implements the logic for running the invariant.
        /// </summary>
        protected abstract (bool, ResultAssertion?) RunInvariant(ITypedElement input, ValidationContext vc);

        /// <inheritdoc />
        public ResultAssertion Validate(ITypedElement input, ValidationContext vc, ValidationState _)
        {
            var (success, directAssertion) = RunInvariant(input, vc);

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

                return ResultAssertion.FromEvidence(new IssueAssertion(sev == IssueSeverity.Error ?
                        Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT :
                        Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT,
                        input.Location, $"Instance failed constraint {getDescription()}"));
            }
            else
                return ResultAssertion.SUCCESS;
        }

        private string getDescription()
        {
            var desc = Key;

            if (!string.IsNullOrEmpty(HumanDescription))
                desc += " \"" + HumanDescription + "\"";

            return desc;
        }
    }
}