/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public static class AssertionExtensions
    {
        public static Assertions AddResultAssertion(this Assertions assertions)
        {
            return assertions.OfType<IssueAssertion>().Any() ? assertions + ResultAssertion.FAILURE : assertions + ResultAssertion.SUCCESS;
        }

        public static IEnumerable<IssueAssertion> GetIssueAssertions(this Assertions assertions)
            => assertions.OfType<IssueAssertion>().Concat(assertions.Result.Evidence.OfType<IssueAssertion>());
    }
}
