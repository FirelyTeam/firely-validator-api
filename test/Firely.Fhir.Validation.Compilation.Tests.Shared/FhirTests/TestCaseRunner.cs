﻿using FluentAssertions;
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
        OutputTextAssertion = 1 << 2
    }

    internal class TestCaseRunner
    {
        private static readonly StructureDefinitionSummaryProvider SDPROVIDER = new(new CachedResolver(ZipSource.CreateValidationSource()));

        public (OperationOutcome, OperationOutcome?) RunTestCase(TestCase testCase, ITestValidator engine, string baseDirectory, AssertionOptions options = AssertionOptions.OutputTextAssertion)
            => RunTestCaseAsync(testCase, engine, baseDirectory, options);

        public (OperationOutcome, OperationOutcome?) RunTestCaseAsync(TestCase testCase, ITestValidator engine, string baseDirectory, AssertionOptions options = AssertionOptions.OutputTextAssertion)
        {
            if (engine.CannotValidateTest(testCase)) return (new OperationOutcome(), default(OperationOutcome));

            var absolutePath = Path.GetFullPath(baseDirectory);
            var testResource = parseResource(Path.Combine(absolutePath, testCase.FileName!));

            OperationOutcome? outcomeWithProfile = null;
            if (testCase.Profile?.Source is { } source)
            {
                var profileResource = parseResource(Path.Combine(absolutePath, source));
                var profileUri = profileResource?.InstanceType == "StructureDefinition" ? profileResource.Children("url").SingleOrDefault()?.Value as string : null;

                Assert.IsNotNull(profileUri, $"Could not find url in profile {source}");

                var supportingFiles = (testCase.Profile.Supporting ?? Enumerable.Empty<string>())
                    .Concat(testCase.Supporting ?? Enumerable.Empty<string>())
                    .Concat(testCase.Profiles ?? Enumerable.Empty<string>())
                    .Concat(new[] { source });
                var resolver = buildTestContextResolver(absolutePath, supportingFiles);
                outcomeWithProfile = engine.Validate(testResource, resolver, profileUri);
                assertResult(engine.GetExpectedResults(testCase.Profile), outcomeWithProfile, options);
            }

            var supportFiles = (testCase.Supporting ?? Enumerable.Empty<string>()).Concat(testCase.Profiles ?? Enumerable.Empty<string>());
            var contextResolver = buildTestContextResolver(absolutePath, supportFiles);
            OperationOutcome outcome = engine.Validate(testResource, contextResolver, null);
            assertResult(engine.GetExpectedResults(testCase), outcome, options);

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

                        engine.SetExpectedResults(testCase, outcome.ToExpectedResults());
                        if (outcomeProfile is not null)
                        {
                            engine.SetExpectedResults(testCase.Profile!, outcomeProfile.ToExpectedResults());
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

        private static IResourceResolver? buildTestContextResolver(string baseDirectory, IEnumerable<string> supportingFiles)
        {
            if (supportingFiles.Any())
            {
                // build a resolver made only for this test
                var testContextResolver = new DirectorySource(
                     baseDirectory,
                     new DirectorySourceSettings { Includes = supportingFiles.ToArray(), IncludeSubDirectories = true }
                 );
                return testContextResolver;
            }

            return null;
        }

        private static void assertResult(ExpectedResult? result, OperationOutcome outcome, AssertionOptions options)
        {
            if (options.HasFlag(AssertionOptions.NoAssertion)) return; // no assertion asked

            outcome.RemoveDuplicateMessages();

            result.Should().NotBeNull("There should be an expected result");

            Assert.AreEqual(result!.ErrorCount ?? 0, outcome.Errors + outcome.Fatals, errorsWarnings(result, outcome));
            Assert.AreEqual(result.WarningCount ?? 0, outcome.Warnings, errorsWarnings(result, outcome));

            if (options.HasFlag(AssertionOptions.OutputTextAssertion))
            {
                outcome.Issue.Select(i => i.ToString()).ToList().Should().BeEquivalentTo(result.Output ?? new());
            }

            static string errorsWarnings(ExpectedResult expected, OperationOutcome actual) =>
                $"Errors: {actual.Errors + actual.Fatals} (expected {expected.ErrorCount}), " +
                    $"Warnings: {actual.Warnings} (expected {expected.WarningCount}) - {actual}";
        }

        private ITypedElement parseResource(string fileName)
        {
            var resourceText = File.ReadAllText(fileName);
            return fileName.EndsWith(".xml")
                   ? FhirXmlNode.Parse(resourceText).ToTypedElement(SDPROVIDER)
                   : FhirJsonNode.Parse(resourceText).ToTypedElement(SDPROVIDER);
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