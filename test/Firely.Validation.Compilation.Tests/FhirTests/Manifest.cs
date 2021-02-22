using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Firely.Validation.Compilation.Tests
{
    public class ExpectedResult
    {
        [JsonPropertyName("errorCount")]
        public int? ErrorCount { get; set; }

        [JsonPropertyName("output")]
        public List<string>? Output { get; set; }

        [JsonPropertyName("warningCount")]
        public int? WarningCount { get; set; }

        [JsonPropertyName("todo")]
        public string? Todo { get; set; }

        [JsonPropertyName("infoCount")]
        public int? InfoCount { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("java")]
        public ExpectedResult? Java { get; set; }

        [JsonPropertyName("firely-sdk")]
        public ExpectedResult? FirelySDK { get; set; }

        [JsonPropertyName("supporting")]
        public List<string>? Supporting { get; set; }
    }

    public class TestCase
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("file")]
        public string? FileName { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("java")]
        public ExpectedResult? Java { get; set; }

        [JsonPropertyName("firely-sdk")]
        public ExpectedResult? FirelySDK { get; set; }

        [JsonPropertyName("profiles")]
        public List<string>? Profiles { get; set; }

        [JsonPropertyName("profile")]
        public Profile? Profile { get; set; }

        [JsonPropertyName("supporting")]
        public List<string>? Supporting { get; set; }

        [JsonPropertyName("allowed-extension-domain")]
        public string? AllowedExtensionDomain { get; set; }
    }

    public class Manifest
    {
        [JsonPropertyName("documentation")]
        public List<string>? Documentation { get; set; }

        [JsonPropertyName("test-cases")]
        public List<TestCase>? TestCases { get; set; }
    }

}
