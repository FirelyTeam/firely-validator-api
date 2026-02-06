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
    // Probably need to update CompareToCorrectSchemaSnaps instead of this test, but I don't know how.
    public class StructureDefinitionCorrectionsResolverTests
    {
        [Fact]
        [Trait("Category", "Validation")]
        public async Task ResourceIdCorrectionTest()
        {
            var context = new ResourceIdCorrectionTestContext("hl7.fhir.r4b.core@4.3.0");
            var correctingResolver = new StructureDefinitionCorrectionsResolver(context.PackageResolver);

            await context.Test(correctingResolver, "http://hl7.org/fhirpath/System.String", "id");
        }
    }
}