﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a textual debug message, without influencing the outcome of other assertions.
    /// </summary>
    [DataContract]
    public class TraceAssertion : IValidatable
    {

#if MSGPACK_KEY
        /// <summary>
        /// The human-readable location for the message.
        /// </summary>
        [DataMember(Order = 0)]
        public string Location { get; }

        /// <summary>
        /// The trace message with diagnostic information.
        /// </summary>
        [DataMember(Order = 1)]
        public string Message { get; private set; }
#else
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
#endif

        /// <summary>
        /// Create an trace with a message and location.
        /// </summary>
        public TraceAssertion(string location, string message)
        {
            Location = location;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <inheritdoc />
        public Task<ResultAssertion> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            // Validation does not mean anything more than using this instance as a prototype and
            // turning the trace assertion into a result by cloning the prototype and setting the
            // runtime location.  Note that this is only done when Validate() is called, which is when
            // this assertion is part of a generated schema (e.g. as a trace in a slice),
            // not when instances of TraceAssertion are used as results.
            var clone = new TraceAssertion(input.Location, Message);
            return Task.FromResult(ResultAssertion.FromEvidence(clone));
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("trace", new JObject(new JProperty("message", Message)));
    }
}
