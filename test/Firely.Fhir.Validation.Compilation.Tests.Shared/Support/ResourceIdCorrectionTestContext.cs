/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    /// <summary>
    /// CompareToCorrectSchemaSnaps cannot check fhir type extensions so we do it manually.
    /// </summary>
    public class ResourceIdCorrectionTestContext(params string[] packageNames) : TestContext(packageNames)
    {
        public async Task RunTest(IAsyncResourceResolver correctingResolver, string typeCode, string fhirTypeValue, int extensionCount = 1)
        {
            // Check uncorrected resource
            await checkResource(PackageResolver, typeCode, extensionCount, fhirTypeValue);

            // Check corrected resource: type code and fhir type extension value should both be "id"
            await checkResource(correctingResolver, "id", extensionCount, "id");
        }

        private static async Task checkResource(IAsyncResourceResolver resolver, string expectedTypeCode, int extensionCount, string? expectedFhirTypeValue)
        {
            const string resource = "http://hl7.org/fhir/StructureDefinition/Resource";
            const string fhirTypeExtension = "http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type";

            var result = await resolver.ResolveByCanonicalUriAsync(resource);

            result.Should().NotBeNull();
            result.Should().BeOfType<StructureDefinition>();

            var sd = (StructureDefinition)result;
            var idElements = sd.Differential.Element.Where(e => e.ElementId == "Resource.id").ToList();

            idElements.Should().NotBeEmpty();

            foreach (var idElement in idElements.Where(sce => sce.Type.Count == 1))
            {
                idElement.Type[0].Code.Should().Be(expectedTypeCode);

                var fhirTypeExtensions = idElement.Type[0].Extension.Where(e => e.Url == fhirTypeExtension).ToList();

                fhirTypeExtensions.Should().HaveCount(extensionCount);

                foreach (var extension in fhirTypeExtensions)
                {
                    extension.Value.Should().BeAssignableTo<IValue<string>>();

                    var fhirUrl = (IValue<string>) extension.Value;

                    fhirUrl.Value.Should().Be(expectedFhirTypeValue);
                }
            }
        }
    }
}