﻿using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class IssueAssertion : IAssertion, IValidatable
    {
        public int IssueNumber { get; }
        public string? Location { get; private set; }
        public IssueSeverity? Severity { get; }
        public string Message { get; }
        public IssueType Type { get; }


        public IssueAssertion(Issue issue, string? location, string message) :
            this(issue.IssueNumber, location, message, issue.Severity, issue.Type)
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
            Type = Type;
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

        public Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            // update location
            Location = input.Location;
            return Task.FromResult(new Assertions(this));
        }
    }
}
