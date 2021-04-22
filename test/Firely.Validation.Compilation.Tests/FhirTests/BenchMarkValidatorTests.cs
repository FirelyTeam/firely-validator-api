using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class BenchMarkValidatorTests
    {
        private TestCaseRunner? _runner;
        private ITestValidator? _wipValidator;
        private ITestValidator? _currentValidator;
        private static TestContext? _testContext;
        private static StringBuilder _testOutput;

        private readonly static IResourceResolver ZIPSOURCE = new CachedResolver(ZipSource.CreateValidationSource());
        private readonly static IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);

        private readonly static string TEST_DIRECTORY = Path.GetFullPath(@"..\..\..\TestData\DocumentComposition");

        private readonly Stopwatch _validatorStopWatch = new();

        [ClassInitialize]
        public static void TestFixtureSetup(TestContext context)
        {
            _testContext = context;
            _testOutput = new();
        }

        [TestInitialize]
        public void Setup()
        {
            _runner = new(ZIPSOURCE, PROVIDER);

            var contextResolver = new CachedResolver(
                    new SnapshotSource(new MultiResolver(new DirectorySource(TEST_DIRECTORY), ZIPSOURCE)));

            var schemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(contextResolver);

            _currentValidator = new CurrentValidator(contextResolver, _validatorStopWatch);
            _wipValidator = new WipValidator(schemaResolver, contextResolver, _validatorStopWatch);
        }

        [TestMethod]
        public async Task CurrentValidatorBenchmark()
                  => await poormansBenchmarking(50, _currentValidator!, nameof(CurrentValidatorBenchmark));

        [TestMethod]
        public async Task WipValidatorBenchmark()
                  => await poormansBenchmarking(50, _wipValidator!, nameof(WipValidatorBenchmark));

        private async Task poormansBenchmarking(int repeat, ITestValidator validator, string benchmarkName)
        {
            // Create Testcase:
            TestCase testcase = new()
            {
                Name = "DocumentBundleComposition",
                FileName = "MainBundle.bundle.xml",
                Version = "4.0",
                Supporting = new()
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
                }
            };

            _testOutput.AppendLine("Timings for: " + benchmarkName);
            _testOutput.AppendLine("-----------------------------");
            // warming up:
            _validatorStopWatch.Reset();
            var stopwatch = Stopwatch.StartNew();
            await runTestcase(testcase, validator);
            stopwatch.Stop();
            _testOutput!.AppendLine($"Warming up testcase: {stopwatch.ElapsedMilliseconds}ms");
            _testOutput.AppendLine($"- only the validator: {_validatorStopWatch.ElapsedMilliseconds}ms");
            _testOutput.AppendLine();

            // running the test N-times:
            _validatorStopWatch.Reset();
            stopwatch.Restart();
            for (int i = 0; i < repeat; i++)
            {
                await runTestcase(testcase, validator);
            }
            stopwatch.Stop();
            _validatorStopWatch.Stop();

            // Write results:
            _testOutput.AppendLine($"Per testcase (N = {repeat}): {stopwatch.ElapsedMilliseconds / repeat}ms");
            _testOutput.AppendLine($"- only the validator:  {_validatorStopWatch.ElapsedMilliseconds / repeat}ms");
            _testOutput.AppendLine();

            Console.WriteLine(_testOutput.ToString());

            async Task runTestcase(TestCase testcase, ITestValidator validator)
                => await _runner!.RunTestCaseAsync(testcase, validator, TEST_DIRECTORY, AssertionOptions.NoAssertion);
        }

        [ClassCleanup]
        public static void TestFixtureTearDown()
        {
            // Runs once after all tests in this class are executed. (Optional)
            // Not guaranteed that it executes instantly after all tests from the class.

            // Adding testoutput to the testcontext as a file so we can see it in de Azure Devops environment
            var testOutputFileName = Path.Combine(_testContext!.TestLogsDir, $"{nameof(BenchMarkValidatorTests)}-{DateTime.UtcNow.ToString("yyyyMMddTHHmmss")}.log");
            File.WriteAllText(testOutputFileName, _testOutput.ToString());

            _testContext.AddResultFile(testOutputFileName);
        }
    }
}
