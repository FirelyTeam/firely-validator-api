using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class BenchMarkWipValidator
    {
        private readonly TestCaseRunner _runner;
        private readonly ITestValidator _validator;
        private readonly IElementSchemaResolver _schemaResolver;
        private readonly IAsyncResourceResolver _contextResolver;
        private readonly ITypedElement _testResource;
        private readonly ValidationContext _validationContext;
        private readonly ElementSchema? _schema;


        private readonly static IResourceResolver ZIPSOURCE = new CachedResolver(ZipSource.CreateValidationSource());
        private readonly static IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);


        private readonly static List<string> _supporting = new()
        {
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
        };

        private readonly TestCase _testcase = new()
        {
            Name = "DocumentBundleComposition",
            FileName = "MainBundle.bundle.xml",
            Version = "4.0",
            Supporting = _supporting
        };

        private readonly Stopwatch _validatorStopWatch = new();

        public BenchMarkWipValidator()
        {
            _runner = new(ZIPSOURCE, PROVIDER);

            _contextResolver = new CachedResolver(TestCaseRunner.BuildTestContextResolver(ZIPSOURCE, @"..\..\..\TestData\DocumentComposition", _supporting));

            _schemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(_contextResolver);
            //_validator = new CurrentValidator(contextResolver, _validatorStopWatch);
            _validator = new WipValidator(_schemaResolver, _contextResolver, _validatorStopWatch);

            _testResource = _runner.ParseResource(Path.Combine(@"..\..\..\TestData\DocumentComposition", _testcase.FileName!));

            var schemaUri = new Uri(ResourceIdentity.Core(_testResource.InstanceType).ToString());
            _schema = TaskHelper.Await(() => _schemaResolver!.GetSchema(schemaUri!));
            _validationContext = new ValidationContext(_schemaResolver,
                    new TerminologyServiceAdapter(new LocalTerminologyService(_contextResolver)))
            {
                ExternalReferenceResolver = async u => (await _contextResolver.ResolveByUriAsync(u))?.ToTypedElement(),
                // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                // should be removed from STU3/R4 once we get the new normative version
                // of FP up, which could do comparisons between quantities.
                ExcludeFilter = a => (a is FhirPathValidator fhirPathAssertion && fhirPathAssertion.Key == "rng-2"),
            };

            //var _ = TaskHelper.Await(() => _schema!.Validate(_testResource, _validationContext));
        }

        //[Benchmark]
        public void BenchMarkWip()
        {
            _runner.RunTestCase(_testcase, _validator, @"..\..\..\TestData\DocumentComposition", AssertionOptions.NoAssertion);
        }

        public void BenchMarkCurrent()
        {
            _runner.RunTestCase(_testcase, _validator, @"..\..\..\TestData\DocumentComposition", AssertionOptions.NoAssertion);
        }

        [TestMethod]
        public void PoormanBenchmarking()
        {
            int N = 10;

            _validatorStopWatch.Reset();
            var stopwatch = Stopwatch.StartNew();
            BenchMarkWip();
            stopwatch.Stop();
            Console.WriteLine($"Warming up testcase: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"- only the validator: {_validatorStopWatch.ElapsedMilliseconds}ms");

            _validatorStopWatch.Reset();

            stopwatch.Restart();
            for (int i = 0; i < N; i++)
            {
                BenchMarkWip();
            }
            stopwatch.Stop();
            _validatorStopWatch.Stop();


            Console.WriteLine($"Per testcase (N = {N}): {stopwatch.ElapsedMilliseconds / N}ms");
            Console.WriteLine($"- only the validator:  {_validatorStopWatch.ElapsedMilliseconds / N}ms");
        }

        [TestMethod]
        public async Task OldStyle()
        {
            int N = 100;
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < N; i++)
                await _schema!.Validate(_testResource, _validationContext);
            stopwatch.Stop();

            Console.WriteLine($"exclusive validator:  {stopwatch.ElapsedMilliseconds / N}ms");

        }
    }
}
