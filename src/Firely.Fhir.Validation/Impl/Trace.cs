using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    // TODO: keep them as separate entries
    // TODO: Do something with issues
    // TODO: remove duplicates
    /// <summary>
    /// Represents a textual debug message, without influencing the outcome of other assertions.
    /// </summary>
    [DataContract]
    public class Trace : IAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string Message { get; private set; }
#else
        [DataMember]
        public string Message { get; private set; }
#endif

        public Trace(string message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public JToken ToJson()
        {
            var trace = new JObject(new JProperty("message", Message));

            /*
            if (Details != null)
            {
                trace.Add(new JProperty("code", Details.Code));
                trace.Add(new JProperty("severity", Details.Severity.GetLiteral()));
                trace.Add(new JProperty("category", Details.Type.GetLiteral()));
            }
            */

            return new JProperty("trace", trace);
        }

    }

    //public class TraceDetails
    //{
    //    public readonly string Code;
    //    public readonly IssueSeverity Severity;
    //    public readonly IssueCategory? Category;

    //    public TraceDetails(string code, IssueSeverity severity, IssueCategory? category = null)
    //    {
    //        Code = code ?? throw new ArgumentNullException(nameof(code));
    //        Severity = severity;
    //        Category = category;
    //    }

    //    public enum IssueSeverity
    //    {
    //        Fatal,
    //        Error,
    //        Warning,
    //        Information
    //    }

    //    public enum IssueCategory
    //    {
    //        /// <summary>
    //        /// Content invalid against the specification or a profile.
    //        /// </summary>
    //        Invalid,

    //        BusinessRule,

    //        Incomplete,

    //        NotSupported,

    //        Exception,

    //        CodeInvalid
    //    }
    //}
}
