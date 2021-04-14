/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class ValidationManifestTest
    {
        [Flags]
        private enum AssertionOptions
        {
            NoAssertion = 1 << 1,
            OutputTextAssertion = 1 << 2
        }

        private record ValidatorEngine
            (
                Func<IValidatorEngines, ExpectedResult?> GetExpectedResults,
                Action<IValidatorEngines, ExpectedResult> SetExpectedResults,
                Func<ITypedElement, IEnumerable<string>, string?, OperationOutcome> Validator,
                IEnumerable<string> IgnoreTests
            );

        private const string TEST_CASES_BASE_PATH = @"FhirTestCases\validator";
        private const string TEST_CASES_MANIFEST = TEST_CASES_BASE_PATH + @"\manifest.json";

        private readonly static IAsyncResourceResolver? ZIPSOURCE = new CachedResolver(ZipSource.CreateValidationSource());
        private readonly static IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);
        private readonly static IElementSchemaResolver STANDARD_SCHEMAS = StructureDefinitionToElementSchemaResolver.CreatedCached(ZIPSOURCE);

        private readonly static ValidatorEngine FIRELY_SDK_CURRENT_VALIDATORENGINE =
            new(tc => tc.FirelySDKCurrent, (tc, result) => tc.FirelySDKCurrent = result, firelySDKCurrentValidator, new[] { "message", "message-empty-entry", "cda/example", "cda/example-no-styles" });
        private readonly static ValidatorEngine FIRELY_SDK_WIP_VALIDATORENGINE =
            new(tc => tc.FirelySDKWip, (tc, result) => tc.FirelySDKWip = result, schemaValidator, new[] { "cda/example", "cda/example-no-styles" });

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Java expectation
        /// </summary>
        /// <param name="testCase"></param>
        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST)]
        public void TestValidationManifest(TestCase testCase) => runTestCase(testCase, FIRELY_SDK_WIP_VALIDATORENGINE);

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Java expectation. Running only 
        /// a single test, using the argument singleTest in the ValidationManifestDataSource annotation
        /// </summary>
        /// <param name="testCase"></param>
        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST, singleTest: "nl/nl-core-patient-01")]
        public void RunSingleTest(TestCase testCase) => runTestCase(testCase, FIRELY_SDK_WIP_VALIDATORENGINE);

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Firely SDK expectation.
        /// This is the base line for the Validator engine in this solution. Any failures of this tests will come from a change in the validator.
        /// In this unit test projects we have 3 manifest for the Firely SDK:
        /// - TestData\manifest-with-firelysdk2-0-results.json: expected results for the validator engine in the current SDK release (2.*)
        /// - TestData\manifest-with-firelysdk2-1-results.json: expected results for the validator engine used in branch refactor/validator of the SDK
        /// - TestData\manifest-with-firelysdk3-0-results.json: expected results for the validator engine in this solution 
        /// </summary>
        /// <param name="testCase"></param>
        [DataTestMethod]
        [ValidationManifestDataSource(@"TestData\manifest-with-firelysdk3-0-results.json",
            ignoreTests:
            new[]
            {
                // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
                "cda/example", "cda/example-no-styles"
            })]
        public void RunFirelySdkWipTests(TestCase testCase) => runTestCase(testCase, FIRELY_SDK_WIP_VALIDATORENGINE, AssertionOptions.OutputTextAssertion);

        [DataTestMethod]
        [ValidationManifestDataSource(@"TestData\manifest-with-firelysdk3-0-results.json",
            ignoreTests:
            new[]
            {
                // Current validator cannot handle circular references
                "message", "message-empty-entry",

                // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
                "cda/example", "cda/example-no-styles"
            })]
        public void RunFirelySdkCurrentTests(TestCase testCase) => runTestCase(testCase, FIRELY_SDK_CURRENT_VALIDATORENGINE, AssertionOptions.OutputTextAssertion);

        private static (OperationOutcome, OperationOutcome?) runTestCase(TestCase testCase, ValidatorEngine engine, AssertionOptions options = AssertionOptions.OutputTextAssertion)
        {
            var testResource = parseResource(getAbsoluteFilePath(testCase.FileName!));

            OperationOutcome? outcomeWithProfile = null;
            if (testCase.Profile?.Source is { } source)
            {
                var profileResource = parseResource(getAbsoluteFilePath(source));
                var profileUri = profileResource?.InstanceType == "StructureDefinition" ? profileResource.Children("url").SingleOrDefault()?.Value as string : null;

                Assert.IsNotNull(profileUri);

                var supportingFiles = (testCase.Profile.Supporting ?? Enumerable.Empty<string>()).Concat(new[] { source });
                outcomeWithProfile = engine.Validator(testResource, supportingFiles, profileUri);
                assertResult(engine.GetExpectedResults(testCase.Profile), outcomeWithProfile, options);
            }

            var supportFiles = (testCase.Supporting ?? Enumerable.Empty<string>()).Concat(testCase.Profiles ?? Enumerable.Empty<string>());

            OperationOutcome outcome = engine.Validator(testResource, supportFiles, null);
            assertResult(engine.GetExpectedResults(testCase), outcome, options);

            return (outcome, outcomeWithProfile);
        }

        private static string getAbsoluteFilePath(string relativeFile)
            => @$"{TEST_CASES_BASE_PATH}\{relativeFile}";

        private static void assertResult(ExpectedResult? result, OperationOutcome outcome, AssertionOptions options)
        {
            if (options.HasFlag(AssertionOptions.NoAssertion)) return; // no assertion asked

            removeDuplicateMessages(outcome);

            result.Should().NotBeNull("There should be an expected result");

            Assert.AreEqual(result!.ErrorCount ?? 0, outcome.Errors + outcome.Fatals, outcome.ToString());
            Assert.AreEqual(result.WarningCount ?? 0, outcome.Warnings, outcome.ToString());

            if (options.HasFlag(AssertionOptions.OutputTextAssertion))
            {
                outcome.Issue.Select(i => i.ToString()).ToList().Should().BeEquivalentTo(result.Output);
            }
        }

        private static ITypedElement parseResource(string fileName)
        {
            var resourceText = File.ReadAllText(fileName);

            return fileName.EndsWith(".xml")
                   ? FhirXmlNode.Parse(resourceText).ToTypedElement(PROVIDER)
                   : FhirJsonNode.Parse(resourceText).ToTypedElement(PROVIDER);
        }

        private static ExpectedResult writeFirelySDK(OperationOutcome outcome)
        {
            removeDuplicateMessages(outcome);
            return new ExpectedResult
            {
                ErrorCount = outcome.Errors + outcome.Fatals,
                WarningCount = outcome.Warnings,
                Output = outcome.Issue.Select(i => i.ToString()).ToList()
            };
        }

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
        /// <param name="testCase"></param>
        [Ignore]
        [TestMethod]
        public void AddFirelySdkValidatorResults()
                    => addOrEditValidatorResults(TEST_CASES_MANIFEST, new[] { FIRELY_SDK_CURRENT_VALIDATORENGINE, FIRELY_SDK_WIP_VALIDATORENGINE });

        private static void addOrEditValidatorResults(string manifestFileName, IEnumerable<ValidatorEngine> engines, string[]? ignoreTests = null)
        {
            var manifestJson = File.ReadAllText(manifestFileName);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, new JsonSerializerOptions() { AllowTrailingCommas = true });

            foreach (var testCase in manifest.TestCases ?? Enumerable.Empty<TestCase>())
            {
                if (ModelInfo.CheckMinorVersionCompatibility(testCase.Version ?? "5.0") && !shouldIgnoreTest(ignoreTests, testCase.Name))
                {
                    foreach (var engine in engines)
                    {
                        if (!shouldIgnoreTest(engine.IgnoreTests, testCase.Name))
                        {

                            var (outcome, outcomeProfile) = runTestCase(testCase, engine, AssertionOptions.NoAssertion);

                            engine.SetExpectedResults(testCase, writeFirelySDK(outcome));
                            if (outcomeProfile is not null)
                            {
                                engine.SetExpectedResults(testCase.Profile!, writeFirelySDK(outcomeProfile));
                            }
                        }
                    }
                }
            }

            var json = JsonSerializer.Serialize(manifest,
                               new JsonSerializerOptions()
                               {
                                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                   WriteIndented = true,
                                   IgnoreNullValues = true
                               });
            File.WriteAllText(@"..\..\..\TestData\manifest-with-firelysdk3-0-results.json", json);

            static bool shouldIgnoreTest(IEnumerable<string>? ignoreTestList, string? test) => ignoreTestList?.Contains(test) ?? false;
        }

        [Ignore]
        [TestMethod]
        public void RoundTripTest()
        {
            var expected = File.ReadAllText(@"TestData\validation-test-suite\manifest.json");
            var manifest = JsonSerializer.Deserialize<Manifest>(expected, new JsonSerializerOptions() { AllowTrailingCommas = true });
            manifest.Should().NotBeNull();
            manifest.TestCases.Should().NotBeNull();
            manifest.TestCases.Should().HaveCountGreaterThan(0);

            /*
            var actual = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IgnoreNullValues = true
                });
            */
            List<string> errors = new();
            //JsonAssert.AreSame("manifest.json", expected, actual, errors);
            errors.Should().BeEmpty();
        }

        /// <summary>
        /// Validator engine based in this solution: the work in progress (wip) validator
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static OperationOutcome schemaValidator(ITypedElement instance, IEnumerable<string> supportingFiles, string? profile = null)
        {
            var outcome = new OperationOutcome();
            var result = Assertions.EMPTY;
            var resolver = buildTestContextResolver(supportingFiles).AsAsync();

            foreach (var profileUri in getProfiles(instance, profile))
            {
                result += validate(instance, profileUri);
            }

            outcome.Add(result.ToOperationOutcome());
            return outcome;

            Assertions validate(ITypedElement typedElement, string canonicalProfile)
            {
                Assertions assertions = Assertions.EMPTY;
                var schemaUri = new Uri(canonicalProfile, UriKind.RelativeOrAbsolute);
                try
                {
                    var schemaResolver = new MultiElementSchemaResolver(STANDARD_SCHEMAS, StructureDefinitionToElementSchemaResolver.CreatedCached(resolver));
                    var schema = TaskHelper.Await(() => schemaResolver.GetSchema(schemaUri));
                    var validationContext = new ValidationContext(schemaResolver,
                            new TerminologyServiceAdapter(new LocalTerminologyService(resolver.AsAsync())))
                    {
                        ExternalReferenceResolver = async u => (await resolver.ResolveByCanonicalUriAsync(u))?.ToTypedElement(),
                        // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                        // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                        // should be removed from STU3/R4 once we get the new normative version
                        // of FP up, which could do comparisons between quantities.
                        ExcludeFilter = a => (a is FhirPathValidator fhirPathAssertion && fhirPathAssertion.Key == "rng-2"),
                    };
                    assertions += TaskHelper.Await(() => schema!.Validate(typedElement, validationContext));
                }
                catch (Exception ex)
                {
                    assertions += new ResultAssertion(ValidationResult.Failure, new IssueAssertion(-1, "", ex.Message, IssueSeverity.Error));
                }
                return assertions;
            }

            IEnumerable<string> getProfiles(ITypedElement node, string? profile = null)
            {
                foreach (var item in node.Children("meta").Children("profile").Select(p => p.Value).Cast<string>())
                {
                    yield return item;
                }
                if (profile is not null)
                {
                    yield return profile;
                }

                var instanceType = ModelInfo.CanonicalUriForFhirCoreType(node.InstanceType).Value;
                if (instanceType is not null)
                {
                    yield return instanceType;
                }
            }
        }

        private static IResourceResolver buildTestContextResolver(IEnumerable<string> supportingFiles)
        {
            if (supportingFiles.Any())
            {
                // build a resolver made only for this test
                var testContextResolver = new DirectorySource(
                     TEST_CASES_BASE_PATH,
                     new DirectorySourceSettings { Includes = supportingFiles.ToArray(), IncludeSubDirectories = true }
                 );
                return new SnapshotSource(new MultiResolver(testContextResolver, ZIPSOURCE));
            }

            return new MultiResolver(ZIPSOURCE); // to make it a IResourceResolver again
        }

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static OperationOutcome firelySDKCurrentValidator(ITypedElement instance, IEnumerable<string> supportingFiles, string? profile = null)
        {
            var resolver = buildTestContextResolver(supportingFiles);
            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = resolver,
                TerminologyService = new LocalTerminologyService(resolver.AsAsync()),
            };

            var validator = new Validator(settings);

            return profile is null ? validator.Validate(instance) : validator.Validate(instance, profile);
        }

        #region ToBeMoved

        // TODO: Remove this when extension method RemoveDuplicateMessages is in Common
        private static OperationOutcome removeDuplicateMessages(OperationOutcome outcome)
        {
            var comparer = new IssueComparer();
            outcome.Issue = outcome.Issue.Distinct(comparer).ToList();
            return outcome;
        }

        // TODO: Remove this when extension method RemoveDuplicateMessages is in Common
        private class IssueComparer : IEqualityComparer<OperationOutcome.IssueComponent>
        {
            public bool Equals(OperationOutcome.IssueComponent? x, OperationOutcome.IssueComponent? y)
            {
                if (x is null && y is null)
                    return true;
                else if (x is null || y is null)
                    return false;
                else return x.Location?.FirstOrDefault() == y.Location?.FirstOrDefault() && x.Details?.Text == y.Details?.Text;
            }

            public int GetHashCode(OperationOutcome.IssueComponent issue)
            {
                var hash = unchecked(issue?.Location?.FirstOrDefault()?.GetHashCode() ^ issue?.Details?.Text?.GetHashCode());
                return (hash is null) ? 0 : hash.Value;
            }

        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal class ValidationManifestDataSourceAttribute : Attribute, ITestDataSource
    {
        private readonly string? _manifestFileName;
        private readonly string? _singleTest;
        private readonly IEnumerable<string> _ignoreTests;

        public ValidationManifestDataSourceAttribute(string manifestFileName, string? singleTest = null, string[]? ignoreTests = null)
        {
            _manifestFileName = manifestFileName;
            _singleTest = singleTest;
            _ignoreTests = ignoreTests ?? Enumerable.Empty<string>();
        }

        public string? GetDisplayName(MethodInfo methodInfo, object[] data)
            => data.FirstOrDefault() is TestCase testCase ? testCase.Name : default;

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            var manifestJson = File.ReadAllText(_manifestFileName);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, new JsonSerializerOptions() { AllowTrailingCommas = true });

            IEnumerable<TestCase> testCases = manifest.TestCases ?? Enumerable.Empty<TestCase>();

            testCases = testCases.Where(t => ModelInfo.CheckMinorVersionCompatibility(t.Version ?? "5.0"));

            if (!string.IsNullOrEmpty(_singleTest))
                testCases = testCases.Where(t => t.Name == _singleTest);
            if (_ignoreTests != null)
                testCases = testCases.Where(t => !_ignoreTests.Contains(t.Name));

            return testCases.Select(e => new object[] { e });
        }
    }
}
