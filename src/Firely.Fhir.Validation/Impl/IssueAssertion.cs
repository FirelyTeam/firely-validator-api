/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts a coded result for the validation that can be used to construct an <see cref="OperationOutcome"/>.
    /// </summary>
    [DataContract]
    public class IssueAssertion : IAssertion, IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public int IssueNumber { get; }

        [DataMember(Order = 1)]
        public string? Location { get; private set; }

        [DataMember(Order = 2)]
        public string Message { get; }

        [DataMember(Order = 3)]
        public IssueSeverity? Severity { get; }

        [DataMember(Order = 4)]
        public IssueType Type { get; }
#else
        [DataMember]
        public int IssueNumber { get; }

        [DataMember]
        public string? Location { get; private set; }

        [DataMember]
        public string Message { get; }

        [DataMember]
        public IssueSeverity? Severity { get; }

        [DataMember]
        public IssueType? Type { get; }
#endif

        public IssueAssertion(Issue issue, string? location, string message) :
            this(issue.Code, location, message, issue.Severity, issue.Type)
        {
        }

        public IssueAssertion(int issueNumber, string message, IssueSeverity? severity = null) :
            this(issueNumber, null, message, severity)
        {
        }

        public IssueAssertion(int issueNumber, string? location, string message, IssueSeverity? severity = null, IssueType? type = null)
        {
            IssueNumber = issueNumber;
            Location = location;
            Severity = severity;
            Message = message;
            Type = type;
        }

        public JToken ToJson()
        {
            var props = new JObject(
                      new JProperty("issueNumber", IssueNumber),
                      new JProperty("severity", Severity),
                      new JProperty("message", Message));
            if (Location != null)
                props.Add(new JProperty("location", Location));
            return new JProperty("issue", props);
        }

        public Task<Assertions> Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            // update location
            Location = input.Location;
            return Task.FromResult(new Assertions(this));
        }
    }
}
