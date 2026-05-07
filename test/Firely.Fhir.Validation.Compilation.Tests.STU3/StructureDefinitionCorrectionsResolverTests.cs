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

            await context.RunTest(correctingResolver, "id", "string", extensionCount: 0);
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task BundleCorrectionTest()
        {
            var context = new BundleCorrectionTestContext("hl7.fhir.r3.core@3.0.2");
            var correctingResolver = new StructureDefinitionCorrectionsResolver(context.ProjectResolver);

            await context.RunTest(correctingResolver, ["bdl-3a", "bdl-3b", "bdl-3c", "bdl-3d", "bdl-10", "bdl-11", "bdl-12", "bdl-15"]);
        }
    }
}