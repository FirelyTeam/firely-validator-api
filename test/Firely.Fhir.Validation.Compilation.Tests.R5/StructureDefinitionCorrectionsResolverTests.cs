/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System.Linq;
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
            var context = new ResourceIdCorrectionTestContext("hl7.fhir.r5.core@5.0.0");
            var correctingResolver = new StructureDefinitionCorrectionsResolver(ModelInfo.ModelInspector, context.PackageResolver);

            await context.Test(correctingResolver, "http://hl7.org/fhirpath/System.String", "id");
        }

        [Fact]
        [Trait("Category", "Validation")]
        public async Task ImagingSelectionCorrectionTest()
        {
            // Arrange
            const string packageServer = "https://packages.simplifier.net";
            
            string[] packageNames = ["hl7.fhir.r5.core@5.0.0"];
            var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
            var correctingResolver = new StructureDefinitionCorrectionsResolver(ModelInfo.ModelInspector, packageResolver);

            // Check uncorrected resource
            await checkImagingSelection(packageResolver, "Coding");

            // Check corrected resource
            await checkImagingSelection(correctingResolver, "id");
        }

        private static async Task checkImagingSelection(IAsyncResourceResolver resolver, string expectedTypeCode)
        {
            const string imagingSelection = "http://hl7.org/fhir/StructureDefinition/ImagingSelection";

            var result = await resolver.ResolveByCanonicalUriAsync(imagingSelection);

            result.Should().NotBeNull();
            result.Should().BeOfType<StructureDefinition>();

            var sd = (StructureDefinition)result;

            var sopClassElements = sd.Differential.Element.Where(e => e.ElementId == "ImagingSelection.instance.sopClass").ToList();

            sopClassElements.Should().NotBeEmpty();

            foreach (var sopClassElement in sopClassElements.Where(sce => sce.Type.Count == 1))
                sopClassElement.Type[0].Code.Should().Be(expectedTypeCode);
        }
    }
}