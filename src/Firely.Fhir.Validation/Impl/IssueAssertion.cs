/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a coded result for the validation that can be used to construct an 
    /// <see cref="Hl7.Fhir.Model.OperationOutcome"/>.
    /// </summary>
    [DataContract]
    public class IssueAssertion : IResultAssertion, IValidatable
    {
        /// <summary>
        /// A constant number identifying the issue.
        /// </summary>
        /// <remarks>This number is used as a code for the <see cref="CodeableConcept"/> assigned
        /// to <see cref="IssueComponent.Details" /> when creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public int IssueNumber { get; }

        /// <summary>
        /// The location of the issue, formulated as a FhirPath expression.
        /// </summary>
        /// <remarks>The location is used to set <see cref="IssueComponent.Location" /> when
        /// creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public string? Location { get; private set; }

        /// <summary>
        /// A human-readable text describing the issue.
        /// </summary>
        /// <remarks>This number is used as a text for the <see cref="CodeableConcept"/> assigned
        /// to <see cref="IssueComponent.Details" /> when creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public string Message { get; }

        /// <summary>
        /// The severity of the issue.
        /// </summary>
        /// <remarks>The severity is used to set <see cref="IssueComponent.Severity" /> when
        /// creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public IssueSeverity? Severity { get; }

        /// <summary>
        /// The kind of issue (e.g. "not supported", "too costly"), chosen from the standardized
        /// set in <see cref="IssueType" />.
        /// </summary>
        /// <remarks>The severity is used to set <see cref="IssueComponent.Code" /> when
        /// creating an <see cref="OperationOutcome" />.</remarks>
        [DataMember]
        public IssueType? Type { get; }

        /// <summary>
        /// Interprets the <see cref="IssueSeverity" /> of the assertion as a <see cref="ValidationResult" />
        /// to be used by the validator for deriving the result of the validation.
        /// </summary>
        public ValidationResult Result =>
            Severity switch
            {
                null or IssueSeverity.Fatal => ValidationResult.Undecided,
                IssueSeverity.Error => ValidationResult.Failure,
                IssueSeverity.Information or IssueSeverity.Warning => ValidationResult.Success,
                _ => throw new NotSupportedException($"Enum values have been added to {nameof(IssueSeverity)} " +
                    $"and the logic for the {nameof(IssueAssertion.Result)} property should be adapted accordingly.")
            };

        /// <summary>
        /// Constructs a new <see cref="IssueAssertion" /> given a predefined <see cref="Issue" />,
        /// with additional information about the location and the message. 
        /// </summary>
        public IssueAssertion(Issue issue, string location, string message) :
            this(issue.Code, location, message, issue.Severity, issue.Type)
        {
        }

        /// <inheritdoc cref="IssueAssertion(int, string?, string, IssueSeverity?, IssueType?)"/>
        public IssueAssertion(int issueNumber, string message, IssueSeverity? severity = null) :
            this(issueNumber, null, message, severity)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="IssueAssertion" /> for which there exists no 
        /// predefined <see cref="Issue" />.
        /// </summary>
        /// <remarks>This overload should be used sparingly (e.g. when no predefined Issue is
        /// yet available), since users of the SDK may depend on fixed, repeatable outcomes
        /// for the same kinds of errors.</remarks>
        public IssueAssertion(int issueNumber, string? location, string message, IssueSeverity? severity = null, IssueType? type = null)
        {
            IssueNumber = issueNumber;
            Location = location;
            Severity = severity;
            Message = message;
            Type = type;
        }

        /// <inheritdoc />
        public JToken ToJson()
        {
            var props = new JObject(
                      new JProperty("issueNumber", IssueNumber),
                      new JProperty("severity", Severity?.ToString()),
                      new JProperty("message", Message));
            if (Location != null)
                props.Add(new JProperty("location", Location));
            return new JProperty("issue", props);
        }

        /// <inheritdoc />
        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            // Validation does not mean anything more than using this instance as a prototype and
            // turning the issue assertion into a result by cloning the prototype and setting the
            // runtime location.  Note that this is only done when Validate() is called, which is when
            // this assertion is part of a generated schema (e.g. the default case in a slice),
            // not when instances of IssueAssertion are used as results.
            var clone = new IssueAssertion(IssueNumber, input.Location, Message, Severity, Type);
            return Task.FromResult(ResultAssertion.FromEvidence(clone));
        }
    }
}
