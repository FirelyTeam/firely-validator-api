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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a coded result for the validation that can be used to construct an 
    /// <see cref="Hl7.Fhir.Model.OperationOutcome"/>.
    /// </summary>
    [DataContract]
    public class IssueAssertion : IFixedResult, IValidatable, IEquatable<IssueAssertion?>
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
        /// A human-readable text describing the issue.
        /// </summary>
        /// <remarks>This number is used as a text for the <see cref="CodeableConcept"/> assigned
        /// to <see cref="IssueComponent.Details" /> when creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// The severity of the issue.
        /// </summary>
        /// <remarks>The severity is used to set <see cref="IssueComponent.Severity" /> when
        /// creating an <see cref="OperationOutcome" />.
        /// </remarks>
        [DataMember]
        public IssueSeverity Severity { get; }

        /// <summary>
        /// The kind of issue (e.g. "not supported", "too costly"), chosen from the standardized
        /// set in <see cref="IssueType" />.
        /// </summary>
        /// <remarks>The severity is used to set <see cref="IssueComponent.Code" /> when
        /// creating an <see cref="OperationOutcome" />.</remarks>
        [DataMember]
        public IssueType? Type { get; }

        /// <summary>
        /// The location of the issue, formulated as a FhirPath expression.
        /// </summary>
        /// <remarks>The location is used to set <see cref="IssueComponent.Location" /> when
        /// creating an <see cref="OperationOutcome" />.
        /// </remarks>
        public string? Location { get; private set; }

        /// <summary>
        /// A reference to the definition (=an element in a StructureDefinition) that raised the issue.
        /// </summary>
        internal DefinitionPath? DefinitionPath { get; private set; }

        /// <summary>
        /// Interprets the <see cref="IssueSeverity" /> of the assertion as a <see cref="ValidationResult" />
        /// to be used by the validator for deriving the result of the validation.
        /// </summary>
        public ValidationResult Result =>
            Severity switch
            {
                IssueSeverity.Fatal => ValidationResult.Undecided,
                IssueSeverity.Error => ValidationResult.Failure,
                IssueSeverity.Information or IssueSeverity.Warning => ValidationResult.Success,
                _ => throw new NotSupportedException($"Enum values have been added to {nameof(IssueSeverity)} " +
                    $"and the logic for the {nameof(IssueAssertion.Result)} property should be adapted accordingly.")
            };

        ValidationResult IFixedResult.FixedResult => Result;

        /// <summary>
        /// Constructs a new <see cref="IssueAssertion" /> given a predefined <see cref="Issue" />,
        /// specifying the warning/error message to convey.
        /// </summary>
        public IssueAssertion(Issue issue, string message) :
            this(issue.Code, message, issue.Severity, issue.Type)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="IssueAssertion" /> for which there exists no 
        /// predefined <see cref="Issue" />.
        /// </summary>
        /// <remarks>This overload should be used sparingly (e.g. when no predefined Issue is
        /// yet available), since users of the SDK may depend on fixed, repeatable outcomes
        /// for the same kinds of errors.</remarks>
        public IssueAssertion(int issueNumber, string message, IssueSeverity severity, IssueType? type = null) :
            this(issueNumber, null, null, message, severity, type)
        {
        }

        private IssueAssertion(int issueNumber, string? location, DefinitionPath? definitionPath, string message, IssueSeverity severity, IssueType? type = null)
        {
            IssueNumber = issueNumber;
            Location = location;
            DefinitionPath = definitionPath;
            Severity = severity;
            Message = message;
            Type = type;
        }

        /// <inheritdoc />
        public JToken ToJson()
        {
            var props = new JObject(
                      new JProperty("issueNumber", IssueNumber),
                      new JProperty("severity", Severity.ToString()),
                      new JProperty("message", Message));
            if (Location != null)
                props.Add(new JProperty("location", Location));
            if (DefinitionPath != null)
                props.Add(new JProperty("specref", DefinitionPath));
            if (Type != null)
                props.Add(new JProperty("type", Type.ToString()));

            return new JProperty("issue", props);

        }

        /// <summary>
        /// The variable patterns that can be used inside error messages used in the issue message. These will be
        /// replaced by their actual values at validation time.
        /// </summary>
        public static class Pattern
        {
            /// <summary>
            /// Will be replaced by <see cref="IBaseElementNavigator{IScopedNode}.InstanceType"/> at runtime.
            /// </summary>
            public const string INSTANCETYPE = "%INSTANCETYPE%";

            /// <summary>
            /// Will be replaced by the url of the resource under validation at runtime.
            /// </summary>
            public const string RESOURCEURL = "%RESOURCEURL%";
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings _, ValidationState state)
        {
            // Validation does not mean anything more than using this instance as a prototype and
            // turning the issue assertion into a result by cloning the prototype and setting the
            // runtime location.  Note that this is only done when Validate() is called, which is when
            // this assertion is part of a generated schema (e.g. the default case in a slice),
            // not when instances of IssueAssertion are used as results.
            // Also, we replace some "magic" tags in the message with common runtime data
            var message = Message.Replace(Pattern.INSTANCETYPE, input.InstanceType).Replace(Pattern.RESOURCEURL, state.Instance.ResourceUrl);

            return new IssueAssertion(IssueNumber, message, Severity, Type).AsResult(state);
        }

        /// <summary>
        /// Package this <see cref="IssueAssertion"/> as a <see cref="ResultReport"/>
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal ResultReport AsResult(ValidationState state) => asResult(state.Location.InstanceLocation.ToString(), state.Location.DefinitionPath);

        /// <summary>
        /// Package this <see cref="IssueAssertion"/> as a <see cref="ResultReport"/>
        /// </summary>
        public ResultReport AsResult(string location) => asResult(location, default);


        /// <summary>
        /// Package this <see cref="IssueAssertion"/> as a <see cref="ResultReport"/>
        /// </summary>
        private ResultReport asResult(string location, DefinitionPath? definitionPath) =>
            new(Result, new IssueAssertion(IssueNumber, location, definitionPath, Message, Severity, Type));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as IssueAssertion);

        /// <inheritdoc/>
        public bool Equals(IssueAssertion? other) => other is not null &&
            IssueNumber == other.IssueNumber &&
            Location == other.Location &&
            Message == other.Message &&
            Severity == other.Severity &&
            Type == other.Type;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(IssueNumber, Location, Message, Severity, Type, Result);

        /// <inheritdoc/>
        public static bool operator ==(IssueAssertion? left, IssueAssertion? right) => EqualityComparer<IssueAssertion>.Default.Equals(left!, right!);

        /// <inheritdoc/>
        public static bool operator !=(IssueAssertion? left, IssueAssertion? right) => !(left == right);
    }
}
