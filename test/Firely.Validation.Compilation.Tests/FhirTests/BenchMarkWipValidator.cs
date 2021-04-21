using BenchmarkDotNet.Attributes;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class BenchMarkWipValidator
    {
        private readonly TestCaseRunner _runner;
        private readonly ITestValidator _validator;

        private readonly static IResourceResolver ZIPSOURCE = new CachedResolver(ZipSource.CreateValidationSource());
        private readonly static IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);


        public BenchMarkWipValidator()
        {
            _runner = new(ZIPSOURCE, PROVIDER);

            var cachedSchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(ZIPSOURCE.AsAsync());
            _validator = new WipValidator(cachedSchemaResolver);
        }

        [Benchmark]
        public void BenchMark()
        {
            TestCase testcase = new()
            {
                Name = "DocumentBundleComposition",
                FileName = "MainBundle.bundle.xml",
                Version = "4.0",
                Supporting = new List<string> {
                    "SectionTitles.valueset.xml",
                    "WeightHeightObservation.structuredefinition.xml",
                    "WeightQuantity.structuredefinition.xml",
                    "DocumentBundle.structuredefinition.xml",
                    "DocumentComposition.structuredefinition.xml",
                    "HeightQuantity.structuredefinition.xml",
                    "patient-clinicalTrial.xml",
                    "Levin.patient.xml",
                    "Weight.observation.xml",
                    "Hippocrates.practitioner.xml"
                }
            };

            _runner.RunTestCase(testcase, _validator, @"..\..\..\TestData\DocumentComposition", AssertionOptions.NoAssertion);
        }
    }
}
