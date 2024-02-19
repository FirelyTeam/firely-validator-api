using Firely.Fhir.Validation.Tests;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Impl.Tests
{
    [TestClass]
    public class FhirStringValidatorTests : BasicValidatorTests
    {
        [DataTestMethod]
        [FhirStringValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
           => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }

    internal class FhirStringValidatorDataAttribute : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new FhirStringValidator(),
                ElementNode.ForPrimitive("correct-string"),
                true, null, "absolure urls are allowed"
            };
            yield return new object?[]
            {
                new FhirStringValidator(),
                ElementNode.ForPrimitive(""),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "Empty strings are not allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive(12),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "Only strings are allowed here"
            };
        }
    }
}