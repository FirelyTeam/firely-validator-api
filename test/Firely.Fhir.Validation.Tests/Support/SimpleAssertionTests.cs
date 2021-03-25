using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    public abstract class SimpleAssertionTests
    {
        public virtual async Task SimpleAssertionTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
        {
            var result = await assertion.Validate(input, ValidationContext.CreateDefault()).ConfigureAwait(false);

            result.Should().NotBeNull();
            result.Result.IsSuccessful.Should().Be(expectedResult, failureMessage);

            if (expectedResult == false && expectedIssue is not null)
            {
                result.Result.Evidence.OfType<IssueAssertion>().Should().Contain(ia => ia.IssueNumber == expectedIssue.Code);
            }
        }
    }
}
