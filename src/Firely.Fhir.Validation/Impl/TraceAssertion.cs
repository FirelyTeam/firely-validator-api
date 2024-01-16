/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a textual debug message, without influencing the outcome of other assertions.
    /// </summary>
    [DataContract]
    public class TraceAssertion : IValidatable, IFixedResult, IEquatable<TraceAssertion?>
    {
        /// <summary>
        /// The human-readable location for the message.
        /// </summary>
        [DataMember]
        public string Location { get; }

        /// <summary>
        /// The trace message with diagnostic information.
        /// </summary>
        [DataMember]
        public string Message { get; private set; }

        /// <inheritdoc />
        public ValidationResult FixedResult => ValidationResult.Success;

        /// <summary>
        /// Create an trace with a message and location.
        /// </summary>
        public TraceAssertion(string location, string message)
        {
            Location = location;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc />
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings _, ValidationState state)
        {
            // Validation does not mean anything more than using this instance as a prototype and
            // turning the trace assertion into a result by cloning the prototype and setting the
            // runtime location.  Note that this is only done when Validate() is called, which is when
            // this assertion is part of a generated schema (e.g. as a trace in a slice),
            // not when instances of TraceAssertion are used as results.
            return new TraceAssertion(state.Location.InstanceLocation.ToString(), Message).AsResult();
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("trace", new JObject(new JProperty("message", Message)));

        /// <summary>
        /// Package this <see cref="IssueAssertion"/> as a <see cref="ResultReport"/>
        /// </summary>
        public ResultReport AsResult() => new(ValidationResult.Success, this);

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as TraceAssertion);

        /// <inheritdoc />
        public bool Equals(TraceAssertion? other) => other is not null &&
            Location == other.Location &&
            Message == other.Message;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Location, Message);

        /// <inheritdoc />
        public static bool operator ==(TraceAssertion? left, TraceAssertion? right) => EqualityComparer<TraceAssertion>.Default.Equals(left!, right!);

        /// <inheritdoc />
        public static bool operator !=(TraceAssertion? left, TraceAssertion? right) => !(left == right);
    }
}
