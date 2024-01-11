/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
    public class FhirTypeLabelValidatorTests : BasicValidatorTests
    {

        [DataTestMethod]
        [FhirTypeLabelValidatorData]
        public override void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
           => base.BasicValidatorTestcases(assertion, input, expectedResult, expectedIssue, failureMessage);
    }
}