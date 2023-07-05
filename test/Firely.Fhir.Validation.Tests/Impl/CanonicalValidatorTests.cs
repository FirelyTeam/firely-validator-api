using Firely.Fhir.Validation.Tests;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Impl.Tests
{
    [TestClass]
    public class CanonicalValidatorTests : BasicValidatorTests
    {
        [DataTestMethod]
        [CanonicalValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
           => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }

    internal class CanonicalValidatorDataAttribute : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("http://fhir.acme.com/Questionnaire/example"),
                true, null, "absolure urls are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("http://fhir.acme.com/Questionnaire/example|1.0"),
                true, null, "absolure urls with versions are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("http://fhir.acme.com/Questionnaire/example|1.0#vs1"),
                true, null, "absolure urls with fragments are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive(12),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "Only strings are allowed here"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("#ref"),
                true, null, "Fragments are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("/relative/canonical"),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "relative canonicals are not allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                ElementNode.ForPrimitive("/relative/canonical#12"),
                true, null, "Fragments are allowed"
            };
        }
    }
}