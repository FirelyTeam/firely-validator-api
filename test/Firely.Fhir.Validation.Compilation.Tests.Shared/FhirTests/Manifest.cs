/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public interface IValidatorEnginesResults
    {
        public ExpectedOutcome? Java { get; set; }

        public ExpectedOutcome? FirelyDotNet { get; set; }
    }

    public class ExpectedOutcome
    {
        [JsonPropertyName("outcome")]
        public JsonObject? Outcome { get; set; }
        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    public class ExpectedResult
    {
        [JsonPropertyName("errorCount")]
        public int? ErrorCount { get; set; }

        [JsonPropertyName("warningCount")]
        public int? WarningCount { get; set; }

        [JsonPropertyName("infoCount")]
        public int? InfoCount { get; set; }

        [JsonPropertyName("debug")]
        public bool? Debug { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("output")]
        public List<string>? Output { get; set; }

        [JsonPropertyName("todo")]
        public string? Todo { get; set; }
    }

    public class Profile : IValidatorEnginesResults
    {
        [JsonPropertyName("assumeValidRestReferences")]
        public bool? AssumeValidRestReferences { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("packages")]
        public string[]? Packages { get; set; }

        // Is this needed? Should be a part of ExpectedResult
        [JsonPropertyName("errorCount")]
        public int? ErrorCount { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("supporting")]
        public List<string>? Supporting { get; set; }

        [JsonPropertyName("java")]
        public ExpectedOutcome? Java { get; set; }

        [JsonPropertyName("firely-dotnet")]
        public ExpectedOutcome? FirelyDotNet { get; set; }
    }

    public class BundleParameter
    {
        [JsonPropertyName("rule")]
        public string? Rule { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }
    }

    public class Logical
    {
        [JsonPropertyName("supporting")]
        public List<string>? Supporting { get; set; }

        [JsonPropertyName("packages")]
        public string[]? Packages { get; set; }

        [JsonPropertyName("expressions")]
        public string[]? Expressions { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("java")]
        public ExpectedOutcome? Java { get; set; }

        [JsonPropertyName("firely-dotnet")]
        public ExpectedOutcome? FirelyDotNet { get; set; }
    }

    public class TestCase : IValidatorEnginesResults
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("file")]
        public string? FileName { get; set; }

        [JsonPropertyName("default-version")]
        public bool? DefaultVersion { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("documentation")]
        public string? Documentation { get; set; }

        [JsonPropertyName("noHtmlInMarkDown")]
        public bool? NoHtmlInMarkdown { get; set; }

        [JsonPropertyName("for-publication")]
        public string? ForPublication { get; set; }

        [JsonPropertyName("fetcher")]
        public string? Fetcher { get; set; }

        [JsonPropertyName("module")]
        public string? Module { get; set; }

        [JsonPropertyName("allow-comments")]
        public bool? AllowComments { get; set; }

        [JsonPropertyName("no-tx")]
        public bool? NoTx { get; set; }

        [JsonPropertyName("ips")]
        public string? Ips { get; set; }

        [JsonPropertyName("close-up")]
        public bool? CloseUp { get; set; }

        [JsonPropertyName("packages")]
        public List<string>? Packages { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("supporting")]
        public List<string>? Supporting { get; set; }

        [JsonPropertyName("validateReference")]
        public string? ValidateReference { get; set; }

        [JsonPropertyName("validateContains")]
        public string? ValidateContains { get; set; }

        // Is this needed? 
        [JsonPropertyName("x-errors")]
        public string[]? XErrors { get; set; }

        [JsonPropertyName("use-test")]
        public bool? UseTest { get; set; }

        [JsonPropertyName("validate")]
        public string? Validate { get; set; }

        [JsonPropertyName("assumeValidRestReferences")]
        public bool? AssumeValidRestReferences { get; set; }

        [JsonPropertyName("allowed-extension-domain")]
        public string? AllowedExtensionDomain { get; set; }

        // Is this needed? Should be a part of ExpectedResult
        [JsonPropertyName("errorCount")]
        public int? ErrorCount { get; set; }

        [JsonPropertyName("best-practice")]
        public string? BestPractice { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }


        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("profiles")]
        public List<string>? Profiles { get; set; }

        [JsonPropertyName("bundle-param")]
        public BundleParameter? BundleParam { get; set; }

        [JsonPropertyName("examples")]
        public bool? Examples { get; set; }

        [JsonPropertyName("security-checks")]
        public bool? SecurityChecks { get; set; }

        [JsonPropertyName("debug")]
        public bool? Debug { get; set; }

        [JsonPropertyName("generate-shapshot")]
        public bool? GenerateSnapshot { get; set; }

        [JsonPropertyName("tx-dependent")]
        public bool? TxDependent { get; set; }

        [JsonPropertyName("txLog")]
        public string? TxLogFileName { get; set; }

        [JsonPropertyName("crumb-trail")]
        public bool? CrumbTrail { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("logical")]
        public Logical? Logical { get; set; }

        [JsonPropertyName("java")]
        public ExpectedOutcome? Java { get; set; }

        [JsonPropertyName("firely-dotnet")]
        public ExpectedOutcome? FirelyDotNet { get; set; }

        [JsonPropertyName("profile")]
        public Profile? Profile { get; set; }
    }

    public class Manifest
    {
        [JsonPropertyName("documentation")]
        public List<string>? Documentation { get; set; }

        [JsonPropertyName("modules")]
        public Modules? Modules { get; set; }

        [JsonPropertyName("test-cases")]
        public List<TestCase>? TestCases { get; set; }
    }

    public class Modules
    {
        [JsonPropertyName("(default)")]
        public string? Default { get; set; }

        [JsonPropertyName("cda")]
        public string? Cda { get; set; }

        [JsonPropertyName("cdshooks")]
        public string? CdsHooks { get; set; }

        [JsonPropertyName("shc")]
        public string? Hhc { get; set; }

        [JsonPropertyName("notes:")]
        public string? Notes { get; set; }

        [JsonPropertyName("json5")]
        public string? Json5 { get; set; }
    }
}
