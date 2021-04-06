using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Assertion about the stated instance type of an element.
    /// </summary>
    [DataContract]
    public class FhirTypeLabel : SimpleAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string Label { get; private set; }
#else
        [DataMember]
        public string Label { get; private set; }
#endif

        public FhirTypeLabel(string label)
        {
            Label = label;
        }

        public override string Key => "fhir-type-label";

        public override object Value => Label;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            // TODO use ModelInfo 
            // ModelInfo.IsInstanceTypeFor(input?.InstanceType);

            var result = Assertions.EMPTY;

            result += input?.InstanceType == Label ?
                new ResultAssertion(ValidationResult.Success) :
                ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, input?.Location, $"Type of instance ({input?.InstanceType}) is not valid at location {input?.Location}."));

            return Task.FromResult(result);
        }
    }
}
