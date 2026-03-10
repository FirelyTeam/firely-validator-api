/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class StructureDefinitionCorrectionsResolverTests
    {
        [Fact]
        [Trait("Category", "Validation")]
        public async Task ResourceIdCorrectionTest()
        {
            var context = new ResourceIdCorrectionTestContext("hl7.fhir.r3.core@3.0.2");
            var correctingResolver = new StructureDefinitionCorrectionsResolver(context.PackageResolver);

            await context.Test(correctingResolver, "id", "string", extensionCount: 0);
        }
    }
}