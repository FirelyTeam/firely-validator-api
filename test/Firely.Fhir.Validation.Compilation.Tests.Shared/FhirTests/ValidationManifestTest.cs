/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class ValidationManifestTest
    {
        private const string TESTPROJECT_BASE_PATH = @"..\..\..\..\..\test\Firely.Fhir.Validation.Compilation.Tests.Shared\";
        private const string TEST_CASES_BASE_PATH = TESTPROJECT_BASE_PATH + @"FhirTestCases\validator";
        private const string TEST_CASES_MANIFEST = TEST_CASES_BASE_PATH + @"\manifest.json";
        private const string DOC_COMPOSITION_TEST_CASES_MANIFEST = TESTPROJECT_BASE_PATH + @"TestData\DocumentComposition\manifest.json";
        private readonly TestCaseRunner _runner = new();


        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Firely SDK expectation. Running only 
        /// a single test, using the argument singleTest in the ValidationManifestDataSource annotation
        /// </summary>
        //[Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST, singleTest: "test-input-params-example1")]
        public void RunSingleTest(TestCase testCase, string baseDirectory)
            => _runner.RunTestCase(testCase, WipValidator.Create(), baseDirectory);

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Firely SDK expectation.
        /// This is the base line for the Validator engine in this solution. Any failures of this tests will come from a change in the validator.
        /// </summary>
        /// <param name="testCase">the single testcase to run</param>
        /// <param name="baseDirectory">the base directory of the testcase</param>
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST)]
        public void RunFirelySdkWipTests(TestCase testCase, string baseDirectory)
                => _runner.RunTestCase(testCase, WipValidator.Create(), baseDirectory, AssertionOptions.OutputTextAssertion);

        [DataTestMethod]
        [Ignore("Until we have ported the old SDK-style validator, this test cannot be run anymore")]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST)]
        public void RunFirelySdkCurrentTests(TestCase testCase, string baseDirectory)
             => _runner.RunTestCase(testCase, CurrentValidator.Create(), baseDirectory, AssertionOptions.OutputTextAssertion);

        [DataTestMethod]
        [ValidationManifestDataSource(DOC_COMPOSITION_TEST_CASES_MANIFEST)]
        public void OldExamples(TestCase testCase, string baseDirectory)
           => _runner.RunTestCase(testCase, WipValidator.Create(), baseDirectory, AssertionOptions.OutputTextAssertion);


        /// <summary>
        /// Not really an unit test, but a way to generate Firely SDK results in an existing manifest.
        /// Input params:
        /// - manifestName of the ValidationManifestDataSource annotation: which manifest to use as a base
        /// - engine of runTestCase: which validator to use generate the Firely SDK results. Two possible methods: 
        ///   * FIRELY_SDK_WIP_VALIDATORENGINE (based in this solution)
        ///   * FIRELY_SDK_CURRENT_VALIDATORENGINE (based in the Firely .NET SDK solution)
        /// Output:
        /// - The method `ClassCleanup` will gather all the testcases and serialize those to disk. The filename can be altered in
        /// that method
        /// </summary>
        [TestMethod]
        [Ignore]
        public void AddFirelySdkValidatorResults()
                    => _runner.AddOrEditValidatorResults(TEST_CASES_MANIFEST, new[] { CurrentValidator.Create(), WipValidator.Create() });

        [TestMethod]
        public void RoundTripTest()
        {
            var expected = File.ReadAllText(TEST_CASES_MANIFEST);
            var manifest = JsonSerializer.Deserialize<Manifest>(expected, new JsonSerializerOptions() { AllowTrailingCommas = true });
            manifest.Should().NotBeNull();
            manifest!.TestCases.Should().NotBeNull();
            manifest.TestCases.Should().HaveCountGreaterThan(0);

            var actual = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            JsonDocument docExpected = JsonDocument.Parse(expected);
            JsonDocument docActual = JsonDocument.Parse(actual);

            StringBuilder errors = new();

            JsonAsserter.AssertJsonDocument(docExpected, docActual, logger);

            var list = errors.ToString();
            errors.Length.Should().Be(0);

            void logger(string message) => errors.AppendLine(message);
        }
    }
}
