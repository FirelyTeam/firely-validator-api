/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents a textual debug message, without influencing the outcome of other assertions.
    /// </summary>
    [DataContract]
    public class TraceAssertion : IAssertion
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

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("trace", new JObject(new JProperty("message", Message)));
    }
}
