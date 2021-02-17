using Firely.Fhir.Validation;
using Firely.Validation.Compilation;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
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
using System.Text.Json;
using System.Text.Json.Serialization;
using Issue = Hl7.Fhir.Support.Issue;

namespace Hl7.Fhir.Specification.Tests
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

        private static Validator _testValidator;
        private static DirectorySource _dirSource;
        private static List<TestCase> _testCases = new List<TestCase>(); // only used by AddFirelySdkResults
        private static IElementSchemaResolver _elementSchemaResolver;
        private static ValidationContext _validationContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _dirSource = new DirectorySource(TEST_CASES_BASE_PATH, new DirectorySourceSettings { IncludeSubDirectories = true });
            var zipSource = ZipSource.CreateValidationSource();
            var resolver = new CachedResolver(new SnapshotSource(new MultiResolver(zipSource, _dirSource)));

            _elementSchemaResolver = new ElementSchemaResolver(resolver);

            var settings = ValidationSettings.CreateDefault();
            settings.GenerateSnapshot = true;
            settings.GenerateSnapshotSettings = new Snapshot.SnapshotGeneratorSettings()
            {
                ForceRegenerateSnapshots = true,
                GenerateSnapshotForExternalProfiles = true,
                GenerateElementIds = true
            };
            settings.ResourceResolver = resolver;
            settings.TerminologyService = new LocalTerminologyService(resolver);

            _testValidator = new Validator(settings);

            _validationContext = new ValidationContext()
            {
                ElementSchemaResolver = _elementSchemaResolver,
                ResourceResolver = resolver,
                ResolveExternalReferences = true,
                TerminologyService = new TerminologyServiceAdapter(new LocalTerminologyService(resolver.AsAsync())),
                // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                // should be removed from STU3/R4 once we get the new normative version
                // of FP up, which could do comparisons between quantities.
                ExcludeFilter = a => (a is FhirPathAssertion fhirPathAssertion && fhirPathAssertion.Key == "rng-2"),
            };
        }

        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST, ignoreTests: new[] { "message", "message-empty-entry" })]
        public void TestValidationManifest(TestCase testCase) => runTestCase(testCase, schemaValidator);

        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(@"FhirTestCases\validator\manifest.json", singleTest: "nl/nl-core-patient-01")]
        public void RunSingleTest(TestCase testCase) => runTestCase(testCase, oldValidator);

        [DataTestMethod]
        [ValidationManifestDataSource(@"TestData\manifest-with-firelysdk3-0-results.json", ignoreTests: new[] { "message", "message-empty-entry" })]
        public void RunFirelySdkTests(TestCase testCase) => runTestCase(testCase, schemaValidator, AssertionOptions.FirelySdkAssertion | AssertionOptions.OutputTextAssertion);

        private (OperationOutcome, OperationOutcome?) runTestCase(TestCase testCase, Func<Resource, string?, OperationOutcome> validator, AssertionOptions options = AssertionOptions.JavaAssertion)
        {
            var testResource = parseResource(@$"{TEST_CASES_BASE_PATH}\{testCase.FileName}");

            OperationOutcome? outcomeWithProfile = null;
            if (testCase.Profile?.Source is { } source)
            {
                var profileUri = _dirSource.ListSummaries().First(s => s.Origin.EndsWith(Path.DirectorySeparatorChar + source)).GetConformanceCanonicalUrl();

                outcomeWithProfile = validator(testResource, profileUri);
                assertResult(options.HasFlag(AssertionOptions.JavaAssertion) ? testCase.Profile.Java : testCase.Profile.FirelySDK, outcomeWithProfile, options);
            }

            OperationOutcome outcome = validator(testResource, null);
            assertResult(options.HasFlag(AssertionOptions.JavaAssertion) ? testCase.Java : testCase.FirelySDK, outcome, options);

            return (outcome, outcomeWithProfile);
        }

        private void assertResult(ExpectedResult result, OperationOutcome outcome, AssertionOptions options)
        {
            if (options.HasFlag(AssertionOptions.NoAssertion)) return; // no assertion asked

            RemoveDuplicateMessages(outcome);

            result.Should().NotBeNull("There should be an expected result");

            (outcome.Errors + outcome.Fatals).Should().Be(result.ErrorCount ?? 0);
            outcome.Warnings.Should().Be(result.WarningCount ?? 0);

            if (options.HasFlag(AssertionOptions.OutputTextAssertion))
            {
                outcome.Issue.Select(i => i.ToString()).ToList().Should().BeEquivalentTo(result.Output);
            }
        }

        private Resource parseResource(string fileName)
        {
            var resourceText = File.ReadAllText(fileName);
            var testResource = fileName.EndsWith(".xml") ?
                new FhirXmlParser().Parse<Resource>(resourceText) :
                new FhirJsonParser().Parse<Resource>(resourceText);
            Assert.IsNotNull(testResource);
            return testResource;
        }

        private ExpectedResult writeFirelySDK(OperationOutcome outcome)
        {
            RemoveDuplicateMessages(outcome);
            return new ExpectedResult
            {
                ErrorCount = outcome.Errors + outcome.Fatals,
                WarningCount = outcome.Warnings,
                Output = outcome.Issue.Select(i => i.ToString()).ToList()
            };
        }

        [Ignore]
        [DataTestMethod]
        [ValidationManifestDataSource(TEST_CASES_MANIFEST, ignoreTests: new[] { "message", "message-empty-entry" })]
        public void AddFirelySdkResults(TestCase testCase)
        {
            var (outcome, outcomeProfile) = runTestCase(testCase, schemaValidator, AssertionOptions.NoAssertion);

            testCase.FirelySDK = writeFirelySDK(outcome);
            if (outcomeProfile is not null)
            {
                testCase.Profile.FirelySDK = writeFirelySDK(outcomeProfile);
            }

            _testCases.Add(testCase);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (_testCases.Any())
            {
                var newManifest = new Manifest
                {
                    TestCases = _testCases
                };

                var json = JsonSerializer.Serialize(newManifest,
                                new JsonSerializerOptions()
                                {
                                    WriteIndented = true,
                                    IgnoreNullValues = true
                                });
                File.WriteAllText(@"..\..\..\TestData\validation-test-suite\manifest-with-firelysdk3-0-results.json", json);
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

            var actual = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IgnoreNullValues = true
                });

            List<string> errors = new List<string>();
            //JsonAssert.AreSame("manifest.json", expected, actual, errors);
            errors.Should().BeEmpty();
        }

        private OperationOutcome schemaValidator(Resource instance, string? profile = null)
        {
            var outcome = new OperationOutcome();
            var node = new ScopedNode(instance.ToTypedElement());
            var definitions = getProfiles(node, profile);

            var result = Assertions.EMPTY;
            foreach (var p in definitions)
            {
                var schemaUri = new Uri(p, UriKind.RelativeOrAbsolute);
                var schema = TaskHelper.Await(() => _elementSchemaResolver.GetSchema(schemaUri));
                result += TaskHelper.Await(() => schema.Validate(node, _validationContext));
            }
            outcome.Add(ToOperationOutcome(result));
            return outcome;
        }

        private IEnumerable<string> getProfiles(ITypedElement node, string? profile = null)
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

        private static IssueSeverity? ConvertToSeverity(OperationOutcome.IssueSeverity? severity)
        {
            return severity switch
            {
                OperationOutcome.IssueSeverity.Fatal => IssueSeverity.Fatal,
                OperationOutcome.IssueSeverity.Error => IssueSeverity.Error,
                OperationOutcome.IssueSeverity.Warning => IssueSeverity.Warning,
                _ => IssueSeverity.Information,
            };
        }

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

        private OperationOutcome oldValidator(Resource instance, string? profile = null)
        {
            return profile is null ? _testValidator.Validate(instance) : _testValidator.Validate(instance, profile);
        }

        private static OperationOutcome RemoveDuplicateMessages(OperationOutcome outcome)
        {
            var comparer = new IssueComparer();
            outcome.Issue = outcome.Issue.Distinct(comparer).ToList();
            return outcome;
        }
    }

    internal class IssueComparer : IEqualityComparer<OperationOutcome.IssueComponent>
    {
        public bool Equals(OperationOutcome.IssueComponent x, OperationOutcome.IssueComponent y)
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



    class ValidationManifestDataSourceAttribute : Attribute, ITestDataSource
    {
        private string? _manifestFileName;
        private string? _singleTest;
        private IEnumerable<string> _ignoreTests;

        public ValidationManifestDataSourceAttribute(string manifestFileName, string? singleTest = null, string[]? ignoreTests = null)
        {
            _manifestFileName = manifestFileName;
            _singleTest = singleTest;
            _ignoreTests = ignoreTests;
        }

        public string? GetDisplayName(MethodInfo methodInfo, object[] data)
            => data.FirstOrDefault() is TestCase testCase ? testCase.Name : default;

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            var manifestJson = File.ReadAllText(_manifestFileName);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, new JsonSerializerOptions() { AllowTrailingCommas = true });

            IEnumerable<TestCase> testCases = manifest.TestCases;

            testCases = testCases.Where(t => t.Version != null && ModelInfo.CheckMinorVersionCompatibility(t.Version));

            if (!string.IsNullOrEmpty(_singleTest))
                testCases = testCases.Where(t => t.Name == _singleTest);
            if (_ignoreTests != null)
                testCases = testCases.Where(t => !_ignoreTests.Contains(t.Name));

            return testCases.Select(e => new object[] { e });
        }
    }

    public class ExpectedResult
    {
        [JsonPropertyName("errorCount")]
        public int? ErrorCount { get; set; }

        [JsonPropertyName("output")]
        public List<string> Output { get; set; }

        [JsonPropertyName("warningCount")]
        public int? WarningCount { get; set; }

        [JsonPropertyName("todo")]
        public string Todo { get; set; }

        [JsonPropertyName("infoCount")]
        public int? InfoCount { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("java")]
        public ExpectedResult Java { get; set; }

        [JsonPropertyName("firely-sdk")]
        public ExpectedResult FirelySDK { get; set; }

        [JsonPropertyName("supporting")]
        public List<string> Supporting { get; set; }
    }

    public class TestCase
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("file")]
        public string FileName { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("java")]
        public ExpectedResult Java { get; set; }

        [JsonPropertyName("firely-sdk")]
        public ExpectedResult FirelySDK { get; set; }

        [JsonPropertyName("profiles")]
        public List<string> Profiles { get; set; }

        [JsonPropertyName("profile")]
        public Profile Profile { get; set; }

        [JsonPropertyName("supporting")]
        public List<string> Supporting { get; set; }

        [JsonPropertyName("allowed-extension-domain")]
        public string AllowedExtensionDomain { get; set; }
    }

    public class Manifest
    {
        [JsonPropertyName("documentation")]
        public List<string> Documentation { get; set; }

        [JsonPropertyName("test-cases")]
        public List<TestCase> TestCases { get; set; }
    }

}
