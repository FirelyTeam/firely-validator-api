using Firely.Fhir.Validation;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using static Hl7.Fhir.Model.OperationOutcome;
using Issue = Hl7.Fhir.Support.Issue;

namespace Firely.Validation.Compilation.Tests
{
    [TestClass]
    public class ValidationManifestTest
    {
        [Flags]
        enum AssertionOptions
        {
            NoAssertion = 1 << 1,
            JavaAssertion = 1 << 2,
            FirelySdkAssertion = 1 << 3,
            OutputTextAssertion = 1 << 4
        }

        private const string TEST_CASES_BASE_PATH = @"FhirTestCases\validator";
        private const string TEST_CASES_MANIFEST = TEST_CASES_BASE_PATH + @"\manifest.json";

        private static Validator? _testValidator;
        private static DirectorySource? _dirSource;
        private static readonly List<TestCase> TESTCASES = new(); // only used by AddFirelySdkResults
        private static IElementSchemaResolver? _elementSchemaResolver;
        private static ValidationContext? _validationContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _dirSource = new DirectorySource(TEST_CASES_BASE_PATH, new DirectorySourceSettings { IncludeSubDirectories = true });
            var zipSource = ZipSource.CreateValidationSource();
            var resolver = new CachedResolver(new SnapshotSource(new MultiResolver(zipSource, _dirSource)));

            _elementSchemaResolver = new StructureDefinitionToElementSchemaResolver(resolver);

            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = resolver,
                TerminologyService = new LocalTerminologyService(resolver),
            };

            _testValidator = new Validator(settings);

            _validationContext = new ValidationContext(_elementSchemaResolver,
                new TerminologyServiceAdapter(new LocalTerminologyService(resolver.AsAsync())))
            {
                ExternalReferenceResolver = async u => (await resolver.ResolveByCanonicalUriAsync(u))?.ToTypedElement(),
                // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                // should be removed from STU3/R4 once we get the new normative version
                // of FP up, which could do comparisons between quantities.
                ExcludeFilter = a => (a is FhirPathAssertion fhirPathAssertion && fhirPathAssertion.Key == "rng-2"),
            };
        }

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Java expectation
        /// </summary>
        /// <param name="testCase"></param>
        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST)]
        public void TestValidationManifest(TestCase testCase) => runTestCase(testCase, schemaValidator, AssertionOptions.JavaAssertion);

        /// <summary>
        /// Running the testcases from the repo https://github.com/FHIR/fhir-test-cases, using the Java expectation. Running only 
        /// a single test, using the argument singleTest in the ValidationManifestDataSource annotation
        /// </summary>
        /// <param name="testCase"></param>
        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST, singleTest: "nl/nl-core-patient-01")]
        public void RunSingleTest(TestCase testCase) => runTestCase(testCase, schemaValidator, AssertionOptions.JavaAssertion);

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
            //   singleTest: "hakan-se",
            ignoreTests:
            new[]
            {
                // For unknown reasons. Marten: Why?
                "message", "message-empty-entry", 

                // The discriminator slices twice on type, the second discriminator
                // stepping into a resolve() for a reference. But: the first slice (String)
                // is not a reference and so has no constraint for that second discriminator.
                // We could fix this (see https://github.com/FirelyTeam/firely-net-sdk/issues/1686)
                // For now, disable this test.
                "mixed-type-slicing",

                // requires 'profile' discriminator support...upcoming
                // in PR https://github.com/FirelyTeam/firely-serverless-validator/pull/13,
                // can be re-enabled afterwards.
                "bundle-duplicate-ids-not",
                "bundle-mni-patientOverview-bundle-example1"
            })]
        public void RunFirelySdkTests(TestCase testCase) => runTestCase(testCase, schemaValidator, AssertionOptions.FirelySdkAssertion | AssertionOptions.OutputTextAssertion);

        private static (OperationOutcome, OperationOutcome?) runTestCase(TestCase testCase, Func<Resource, string?, OperationOutcome> validator, AssertionOptions options = AssertionOptions.JavaAssertion)
        {
            var testResource =
                parseResource(@$"{TEST_CASES_BASE_PATH}\{testCase.FileName}");

            OperationOutcome? outcomeWithProfile = null;
            if (testCase.Profile?.Source is { } source)
            {
                var profileUri = _dirSource!.ListSummaries().First(s => s.Origin.EndsWith(Path.DirectorySeparatorChar + source)).GetConformanceCanonicalUrl();

                outcomeWithProfile = validator(testResource, profileUri);
                assertResult(options.HasFlag(AssertionOptions.JavaAssertion) ? testCase.Profile.Java : testCase.Profile.FirelySDK, outcomeWithProfile, options);
            }

            OperationOutcome outcome = validator(testResource, null);
            assertResult(options.HasFlag(AssertionOptions.JavaAssertion) ? testCase.Java : testCase.FirelySDK, outcome, options);

            return (outcome, outcomeWithProfile);
        }

        private static void assertResult(ExpectedResult? result, OperationOutcome outcome, AssertionOptions options)
        {
            if (options.HasFlag(AssertionOptions.NoAssertion)) return; // no assertion asked

            removeDuplicateMessages(outcome);

            result.Should().NotBeNull("There should be an expected result");

            try
            {
                (outcome.Errors + outcome.Fatals).Should().Be(result!.ErrorCount ?? 0);
                outcome.Warnings.Should().Be(result.WarningCount ?? 0);

                if (options.HasFlag(AssertionOptions.OutputTextAssertion))
                {
                    outcome.Issue.Select(i => i.ToString()).ToList().Should().BeEquivalentTo(result.Output);
                }

            }
            catch (Exception)
            {
                Debug.WriteLine(outcome.ToString());
                throw;
            }
        }

        private static Resource parseResource(string fileName)
        {
            var resourceText = File.ReadAllText(fileName);
            var testResource = fileName.EndsWith(".xml") ?
                new FhirXmlParser().Parse<Resource>(resourceText) :
                new FhirJsonParser().Parse<Resource>(resourceText);
            Assert.IsNotNull(testResource);
            return testResource;
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
        /// - validator of runTestCase: which validator to use generate the Firely SDK results. Two possible methods: firelySDK20Validator and schemaValidator (based in this solution)
        /// Output:
        /// - The method `ClassCleanup` will gather all the testcases and serialize those to disk. The filename can be altered in
        /// that method
        /// </summary>
        /// <param name="testCase"></param>
        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST)]
        public void AddFirelySdkResults(TestCase testCase)
        {
            var (outcome, outcomeProfile) = runTestCase(testCase, schemaValidator, AssertionOptions.NoAssertion);

            testCase.FirelySDK = writeFirelySDK(outcome);
            if (outcomeProfile is not null)
            {
                testCase.Profile!.FirelySDK = writeFirelySDK(outcomeProfile);
            }

            TESTCASES.Add(testCase);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (TESTCASES.Any())
            {
                var newManifest = new Manifest
                {
                    TestCases = TESTCASES
                };

                var json = JsonSerializer.Serialize(newManifest,
                                new JsonSerializerOptions()
                                {
                                    WriteIndented = true,
                                    IgnoreNullValues = true
                                });
                File.WriteAllText(@"..\..\..\TestData\manifest-with-firelysdk3-0-results.json", json);
            }
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
            _ = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IgnoreNullValues = true
                });

            List<string> errors = new();
            //JsonAssert.AreSame("manifest.json", expected, actual, errors);
            errors.Should().BeEmpty();
        }

        /// <summary>
        /// Validator engine based in this solution: the 'new' validator
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private OperationOutcome schemaValidator(Resource instance, string? profile = null)
        {
            var outcome = new OperationOutcome();
            var node = new ScopedNode2(instance.ToTypedElement());
            var definitions = getProfiles(node, profile);

            var result = Assertions.EMPTY;
            foreach (var p in definitions)
            {
                var schemaUri = new Uri(p, UriKind.RelativeOrAbsolute);
                var schema = TaskHelper.Await(() => _elementSchemaResolver!.GetSchema(schemaUri));
                result += TaskHelper.Await(() => schema!.Validate(node, _validationContext!));
            }
            outcome.Add(ToOperationOutcome(result));
            return outcome;
        }

        /// <summary>
        ///  Validation engine of the current Firely SDK (2.x)
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static OperationOutcome firelySDK20Validator(Resource instance, string? profile = null)
        {
            return profile is null ? _testValidator.Validate(instance) : _testValidator.Validate(instance, profile);
        }

        private static IEnumerable<string> getProfiles(ITypedElement node, string? profile = null)
        {
            foreach (var item in node.Children("meta").Children("profile").Select(p => p.Value).Cast<string>())
            {
                yield return item;
            }
            if (profile is not null)
            {
                yield return profile!;
            }

            var instanceType = ModelInfo.CanonicalUriForFhirCoreType(node.InstanceType).Value;
            if (instanceType is not null)
            {
                yield return instanceType;
            }
        }


        // TODO: move this to project Firely.Fhir.Validation when OperationOutcome is in Common
        public static OperationOutcome ToOperationOutcome(Assertions assertions)
        {
            var outcome = new OperationOutcome();

            foreach (var item in assertions.GetIssueAssertions())
            {
                var issue = Issue.Create(item.IssueNumber, convertToSeverity(item.Severity), convertToType(item.Type));
                outcome.AddIssue(item.Message, issue, item.Location);
            }
            return outcome;
        }

        // TODO: move this to project Firely.Fhir.Validation when OperationOutcome is in Common
        private static OperationOutcome.IssueSeverity convertToSeverity(IssueSeverity? severity)
        {
            return severity switch
            {
                IssueSeverity.Fatal => OperationOutcome.IssueSeverity.Fatal,
                IssueSeverity.Error => OperationOutcome.IssueSeverity.Error,
                IssueSeverity.Warning => OperationOutcome.IssueSeverity.Warning,
                _ => OperationOutcome.IssueSeverity.Information,
            };
        }

        // TODO: move this to project Firely.Fhir.Validation when OperationOutcome is in Common
        private static OperationOutcome.IssueType convertToType(IssueType? type) => type switch
        {
            IssueType.BusinessRule => OperationOutcome.IssueType.BusinessRule,
            IssueType.Invalid => OperationOutcome.IssueType.Invalid,
            IssueType.Invariant => OperationOutcome.IssueType.Invariant,
            IssueType.Incomplete => OperationOutcome.IssueType.Incomplete,
            IssueType.Structure => OperationOutcome.IssueType.Structure,
            IssueType.NotSupported => OperationOutcome.IssueType.NotSupported,
            IssueType.Informational => OperationOutcome.IssueType.Informational,
            IssueType.Exception => OperationOutcome.IssueType.Exception,
            IssueType.CodeInvalid => OperationOutcome.IssueType.CodeInvalid,
            _ => OperationOutcome.IssueType.Invalid,
        };

        // TODO: move this to project Firely.Fhir.Validation when OperationOutcome is in Common
        private static OperationOutcome removeDuplicateMessages(OperationOutcome outcome)
        {
            var comparer = new IssueComparer();
            outcome.Issue = outcome.Issue.Distinct(comparer).ToList();
            return outcome;
        }

        // TODO: move this to project Firely.Fhir.Validation when OperationOutcome is in Common
        class IssueComparer : IEqualityComparer<OperationOutcome.IssueComponent>
        {
            public bool Equals(OperationOutcome.IssueComponent? x, OperationOutcome.IssueComponent? y)
            {
                if (x is null && y is null)
                    return true;
                else if (x is null || y is null)
                    return false;
                else if (x.Location?.FirstOrDefault() == y.Location?.FirstOrDefault() && x.Details?.Text == y.Details?.Text)
                    return true;
                else
                    return false;
            }

            public int GetHashCode(OperationOutcome.IssueComponent issue)
            {
                var hash = unchecked(issue?.Location?.FirstOrDefault()?.GetHashCode() ^ issue?.Details?.Text?.GetHashCode());
                return (hash is null) ? 0 : hash.Value;
            }

        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    class ValidationManifestDataSourceAttribute : Attribute, ITestDataSource
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

            testCases = testCases.Where(t => t.Version != null && ModelInfo.CheckMinorVersionCompatibility(t.Version));

            if (!string.IsNullOrEmpty(_singleTest))
                testCases = testCases.Where(t => t.Name == _singleTest);
            if (_ignoreTests != null)
                testCases = testCases.Where(t => !_ignoreTests.Contains(t.Name));

            return testCases.Select(e => new object[] { e });
        }
    }
}
