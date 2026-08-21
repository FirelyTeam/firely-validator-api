/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion expressed using FhirPath.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public abstract record InvariantValidator : IValidatable
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

        /// <summary>
        /// When set, replaces the invariant's effective severity when reporting failures, taking
        /// precedence over both the declared <see cref="Severity"/> and the
        /// <see cref="ValidationSettings.ConstraintBestPractices"/> mapping for best-practice
        /// invariants. Since this is an init-only property on the (record) base, a copy of any
        /// invariant with an override can be made using a <c>with</c> expression.
        /// </summary>
        [DataMember]
        public IssueSeverity? SeverityOverride { get; init; }

        ///<inheritdoc cref="IJsonSerializable.ToJson"/>
        public abstract JToken ToJson();
        
        internal record InvariantResult(bool Success, ResultReport? Report, string? Invariant = null);

        /// <summary>
        /// Implements the logic for running the invariant.
        /// </summary>
        internal abstract InvariantResult RunInvariant(PocoNode input, ValidationSettings vc, ValidationState s);

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState s)
        {
            var result = RunInvariant(input, vc, s);

            if (result.Report is not null) return result.Report;

            if (!result.Success)
            {
                // The effective severity: an explicit override wins over both the best-practice
                // mapping and the invariant's declared severity.
                var sev = SeverityOverride ?? (BestPractice
                    ? vc.ConstraintBestPractices switch
                    {
                        ValidateBestPracticesSeverity.Error => (IssueSeverity?)IssueSeverity.Error,
                        ValidateBestPracticesSeverity.Warning => (IssueSeverity?)IssueSeverity.Warning,
                        _ => throw new InvalidOperationException($"Unknown value for enum {nameof(ValidateBestPracticesSeverity)}."),
                    }
                    : Severity);

                return new IssueAssertion(sev == IssueSeverity.Error ?
                        Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT :
                        Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT,
                        $"Instance failed constraint {getDescription()}").AsResult(s, input, nameof(InvariantValidator),
                        this);
            }
            else
                return ResultReport.SUCCESS;

            string getDescription() => Key +
                (!string.IsNullOrEmpty(HumanDescription) ? $" \"{HumanDescription}\"" : null);

        }

        /// <summary>
        /// Builds the ToJson representation of an invariant: the given properties (an empty set
        /// when omitted) under the given name, extended with the <see cref="SeverityOverride"/>
        /// when one is set - so overridden invariants are recognizable in a rendered schema.
        /// </summary>
        private protected JProperty toInvariantJson(string name, JObject? props = null)
        {
            props ??= new JObject();

            if (SeverityOverride is { } so)
                props.Add(new JProperty("severityOverride", so.GetLiteral()));

            return new JProperty(name, props);
        }
    }
}