using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [Flags]
    public enum AssertionOptions
    {
        NoAssertion = 1 << 1,
        OutputTextAssertion = 1 << 2,
        OnlyErrorCount = 2 << 3
    }

    internal class TestCaseRunner
    {
        private readonly StructureDefinitionSummaryProvider _sdprovider;
        private readonly string _testPath;

        internal TestCaseRunner(string testpath)
        {
            _testPath = testpath;
            _sdprovider = new(new CachedResolver(new MultiResolver(ZipSource.CreateValidationSource(), new DirectorySource(contentDirectory: _testPath))));
        }

        public (OperationOutcome, OperationOutcome?) RunTestCase(TestCase testCase, ITestValidator engine, string baseDirectory, AssertionOptions options = AssertionOptions.OutputTextAssertion)
            => RunTestCaseAsync(testCase, engine, baseDirectory, options);

        public (OperationOutcome, OperationOutcome?) RunTestCaseAsync(TestCase testCase, ITestValidator engine, string baseDirectory, AssertionOptions options = AssertionOptions.OutputTextAssertion)
        {
            if (engine.CannotValidateTest(testCase)) return (new OperationOutcome(), default(OperationOutcome));

            var absolutePath = Path.GetFullPath(baseDirectory);

            OperationOutcome outcome;
            ITypedElement? testResource = null;
            try
            {
                testResource = parseResource(Path.Combine(absolutePath, testCase.FileName!));
                var supportFiles = (testCase.Supporting ?? Enumerable.Empty<string>()).Concat(testCase.Profiles ?? Enumerable.Empty<string>());
                var contextResolver = buildTestContextResolver(absolutePath, supportFiles, testCase.Packages);

                outcome = engine.Validate(testResource, contextResolver, null);
                assertResult(engine.GetExpectedOperationOutcome(testCase), outcome, options);
            }
            catch (Exception e) when (e is InvalidOperationException || e is FormatException)
            {
                outcome = new OperationOutcome() { Issue = [new() { Severity = OperationOutcome.IssueSeverity.Fatal, Code = OperationOutcome.IssueType.Invalid, Diagnostics = e.Message }] };
            }
            catch (Exception e) when (e is FileNotFoundException)
            {
                //file is not found, so we can't run the test
                outcome = new OperationOutcome() { Issue = [new() { Severity = OperationOutcome.IssueSeverity.Fatal, Code = OperationOutcome.IssueType.NotFound, Diagnostics = $"File not found: {e.Message}" }] };
            }


            OperationOutcome? outcomeWithProfile = null;
            if (testResource is not null && testCase.Profile?.Source is { } source)
            {
                try
                {
                    string? profileUri;

                    if (source.StartsWith("http://")) //this is a canonical
                    {
                        profileUri = source;
                    }
                    else //we think this is a reference to a local file
                    {
                        var profileResource = parseResource(Path.Combine(absolutePath, source));
                        profileUri = profileResource?.InstanceType == "StructureDefinition" ? profileResource.Children("url").SingleOrDefault()?.Value as string : null;
                    }

                    Assert.IsNotNull(profileUri, $"Could not find url in profile {source}");

                    var supportingFiles = (testCase.Profile.Supporting ?? Enumerable.Empty<string>())
                        .Concat(testCase.Supporting ?? Enumerable.Empty<string>())
                        .Concat(testCase.Profiles ?? Enumerable.Empty<string>())
                        .Concat(new[] { source });
                    var resolver = buildTestContextResolver(absolutePath, supportingFiles, testCase.Profile.Packages);
                    outcomeWithProfile = engine.Validate(testResource, resolver, profileUri);
                    assertResult(engine.GetExpectedOperationOutcome(testCase.Profile), outcomeWithProfile, options);
                }
                catch (Exception e)
                {
                    if (e is System.InvalidOperationException || e is FormatException)
                        outcome = new OperationOutcome() { Issue = [new() { Severity = OperationOutcome.IssueSeverity.Fatal, Code = OperationOutcome.IssueType.Invalid, Diagnostics = e.Message }] };
                    else if (e is System.IO.FileNotFoundException)
                    {
                        //file is not found, so we can't run the test
                        outcome = new OperationOutcome() { Issue = [new() { Severity = OperationOutcome.IssueSeverity.Fatal, Code = OperationOutcome.IssueType.NotFound, Diagnostics = $"File not found: {e.Message}" }] };
                    }
                    else
                        throw;
                }
            }

            return (outcome, outcomeWithProfile);
        }

        public void AddOrEditValidatorResults(string manifestFileName, IEnumerable<ITestValidator> engines)
        {
            var manifestJson = File.ReadAllText(manifestFileName);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, new JsonSerializerOptions() { AllowTrailingCommas = true })!;

            foreach (var testCase in manifest.TestCases ?? Enumerable.Empty<TestCase>())
            {
                if (ModelInfo.CheckMinorVersionCompatibility(testCase.Version ?? "5.0"))
                {
                    foreach (var engine in engines)
                    {
                        Debug.WriteLine($"Engine {engine.Name}, test {testCase.Name}");

                        if (engine.CannotValidateTest(testCase)) continue;

                        var (outcome, outcomeProfile) = RunTestCase(testCase, engine, Path.GetDirectoryName(manifestFileName)!, AssertionOptions.NoAssertion);

                        engine.SetOperationOutcome(testCase, outcome);
                        if (outcomeProfile is not null)
                        {
                            engine.SetOperationOutcome(testCase.Profile!, outcomeProfile);
                        }
                    }
                }
            }

            Debug.WriteLine("Writing outcomes");

            var json = JsonSerializer.Serialize(manifest,
                               new JsonSerializerOptions()
                               {
                                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                   WriteIndented = true,
                                   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                               });
            File.WriteAllText(manifestFileName, json);
        }

        private static MultiResolver? buildTestContextResolver(string baseDirectory, IEnumerable<string> supportingFiles, IEnumerable<string>? packages = null)
        {
            if (!supportingFiles.Any() && (packages is null || !packages.Any()))
            {
                return null;
            }
            var resolver = new MultiResolver();

            if (supportingFiles.Any())
            {
                var testContextResolver = new DirectorySource(
                    baseDirectory,
                    new DirectorySourceSettings { Includes = supportingFiles.ToArray(), IncludeSubDirectories = true }
                );
                resolver.AddSource(testContextResolver);
            }

            if (packages?.Any() == true)
            {
                var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packages.ToArray());
                resolver.AddSource(packageResolver);
            }

            return resolver;
        }

        private static void assertResult(OperationOutcome? result, OperationOutcome outcome, AssertionOptions options)
        {

            if (options.HasFlag(AssertionOptions.NoAssertion)) return; // no assertion asked

            outcome.RemoveDuplicateMessages();

            result.Should().NotBeNull("There should be an expected result");

            Assert.AreEqual(result!.Fatals, outcome.Fatals, errorsWarnings(result, outcome));
            Assert.AreEqual(result.Errors, outcome.Errors, errorsWarnings(result, outcome));
            Assert.AreEqual(result.Warnings, outcome.Warnings, errorsWarnings(result, outcome));

            if (options.HasFlag(AssertionOptions.OutputTextAssertion))
            {
                outcome.Issue.Select(i => i.ToString()).ToList().Should().BeEquivalentTo(result?.Issue.Select(i => i.ToString()).ToList() ?? new());
            }

            static string errorsWarnings(OperationOutcome expected, OperationOutcome actual) =>
                $"Fatals: {actual.Fatals} (expected {expected.Fatals}), " +
                $"Errors: {actual.Errors} (expected {expected.Errors}), " +
                    $"Warnings: {actual.Warnings} (expected {expected.Warnings}) - {actual}";
        }

        private ITypedElement parseResource(string fileName)
        {
            var resourceText = File.ReadAllText(fileName);
            return fileName.EndsWith(".xml")
                   ? FhirXmlNode.Parse(resourceText).ToTypedElement(_sdprovider)
                   : FhirJsonNode.Parse(resourceText).ToTypedElement(_sdprovider);
        }
    }

    public static class OperationOutcomeExtensions
    {
        public static ExpectedResult ToExpectedResults(this OperationOutcome outcome)
        {
            outcome.RemoveDuplicateMessages();
            return new ExpectedResult
            {
                ErrorCount = outcome.Errors + outcome.Fatals,
                WarningCount = outcome.Warnings,
                Output = outcome.Issue.Select(i => i.ToString()).ToList()
            };
        }
    }
}
