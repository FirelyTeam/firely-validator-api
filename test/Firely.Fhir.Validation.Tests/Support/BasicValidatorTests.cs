/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    public abstract class BasicValidatorTests
    {
        public virtual void BasicValidatorTestcases(IAssertion assertion, ITypedElement input, bool expectedResult, Issue? expectedIssue, string failureMessage)
        {
            var result = assertion.Validate(input, ValidationSettings.BuildMinimalContext());

            result.Should().NotBeNull();
            result.IsSuccessful.Should().Be(expectedResult, failureMessage);

            if (expectedResult == false && expectedIssue is not null)
            {
                result.Evidence.OfType<IssueAssertion>().Should().Contain(ia => ia.IssueNumber == expectedIssue.Code);
            }
        }
    }
}
