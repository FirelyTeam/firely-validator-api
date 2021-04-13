/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    internal class FhirTypeLabelValidatorData : BasicValidatorDataAttribute
    {
        public override IEnumerable<object?[]> GetData()
        {
            yield return new object?[]
            {
                new FhirTypeLabelValidator("System.String"),
                ElementNode.ForPrimitive("Value of type System.String"),
                true, null, "Same type"
            };
            yield return new object?[]
            {
                new FhirTypeLabelValidator("string"),
                ElementNode.ForPrimitive(9),
                false, Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, "Not the same type"
            };
        }
    }

    [TestClass]
    public class FhirTypeLabelTests : BasicValidatorTests
    {

        [DataTestMethod]
        [FhirTypeLabelValidatorData]
        public override Task BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
           => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}