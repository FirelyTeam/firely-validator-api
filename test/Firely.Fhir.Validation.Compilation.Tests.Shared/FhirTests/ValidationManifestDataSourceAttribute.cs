using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class ValidationManifestDataSourceAttribute : Attribute, ITestDataSource
    {
        public static readonly string EMPTY_TESTCASE_NAME = "EMPTY_TESTCASE";

        private readonly string _manifestFileName;
        private readonly string? _singleTest;
        private readonly IEnumerable<string> _ignoreTests;

        public ValidationManifestDataSourceAttribute(string manifestFileName, string? singleTest = null, string[]? ignoreTests = null)
        {
            _manifestFileName = manifestFileName;
            _singleTest = singleTest;
            _ignoreTests = ignoreTests ?? Enumerable.Empty<string>();
        }

        public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
            => data?.FirstOrDefault() is TestCase testCase ? testCase.Name : default;

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            var manifestJson = File.ReadAllText(_manifestFileName);
            var baseDirectory = Path.GetDirectoryName(_manifestFileName!);
            var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, new JsonSerializerOptions() { AllowTrailingCommas = true })!;

            IEnumerable<TestCase> testCases = manifest.TestCases ?? Enumerable.Empty<TestCase>();

            testCases = testCases.Where(t => ModelInfo.CheckMinorVersionCompatibility(t.Version ?? "5.0"));

            if (!string.IsNullOrEmpty(_singleTest))
                testCases = testCases.Where(t => t.Name == _singleTest);
            if (_ignoreTests != null)
                testCases = testCases.Where(t => !_ignoreTests.Contains(t.Name));

            return testCases.Any()
                ? testCases.Select(e => new object[] { e, baseDirectory! })
                : new List<object[]> { new object[] { new TestCase { Name = EMPTY_TESTCASE_NAME }, baseDirectory! } };
        }
    }
}