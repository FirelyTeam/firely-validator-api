using Firely.Fhir.Validation.Tests;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
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
        public override void BasicValidatorTestcases(IAssertion assertion, PocoNode input, bool expectedResult, Issue? expectedIssue, string failureMessage)
           => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }

    internal class CanonicalValidatorDataAttribute : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("http://fhir.acme.com/Questionnaire/example"),
                true, null, "absolure urls are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("http://fhir.acme.com/Questionnaire/example|1.0"),
                true, null, "absolure urls with versions are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("http://fhir.acme.com/Questionnaire/example|1.0#vs1"),
                true, null, "absolure urls with fragments are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Integer>(12),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "Only strings are allowed here"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("#ref"),
                true, null, "Fragments are allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("/relative/canonical"),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "relative canonicals are not allowed"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>("/relative/canonical#12"),
                true, null, "Fragments are allowed"
            };
            var absentCanonical = new Hl7.Fhir.Model.Canonical();
            absentCanonical.SetExtension("http://hl7.org/fhir/StructureDefinition/data-absent-reason", new Code("unknown"));
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive(absentCanonical),
                true, null, "an absent value must skip the format check"
            };
            yield return new object?[]
            {
                new CanonicalValidator(),
                PocoNode.ForPrimitive<Hl7.Fhir.Model.Canonical>(""),
                false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a present but empty canonical is invalid"
            };
        }
    }
}