/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
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
