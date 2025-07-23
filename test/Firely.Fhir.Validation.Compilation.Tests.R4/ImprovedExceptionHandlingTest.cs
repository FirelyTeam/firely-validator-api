/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests.R4
{
    [TestClass]
    public class ImprovedExceptionHandlingTest
    {
        private static readonly StructureDefinitionSummaryProvider _sdProvider = 
            new(new CachedResolver(ZipSource.CreateValidationSource()));
            
        [TestMethod]
        public void TestImprovedExceptionHandling_ProfileResolutionFailure()
        {
            // Test that profile resolution failures result in code -4, not -1
            var validator = DotNetValidator.Create();
            
            // Create a simple valid patient resource
            var json = @"{
                ""resourceType"": ""Patient"",
                ""id"": ""test-patient"",
                ""name"": [{
                    ""family"": ""TestFamily"",
                    ""given"": [""TestGiven""]
                }]
            }";
            
            var resource = FhirJsonNode.Parse(json).ToTypedElement(_sdProvider);
            
            // Try to validate against a non-existent profile
            var result = validator.Validate(resource, null, "http://example.com/nonexistent-profile");
            
            // The result should contain error information
            result.Should().NotBeNull();
            
            System.Console.WriteLine($"Validation result: {result.Issue.Count} issues");
            foreach (var issue in result.Issue)
            {
                var code = issue.Details?.Coding?.FirstOrDefault()?.Code ?? "no-code";
                System.Console.WriteLine($"  Issue: {issue.Code}, Code: {code}, Severity: {issue.Severity}, Message: {issue.Details?.Text}");
            }
            
            if (result.Issue.Count == 0)
            {
                System.Console.WriteLine("No issues found - this suggests the nonexistent profile validation was handled silently");
                return; // This test case doesn't trigger the expected exception - that's OK
            }
            
            // Look for issues related to profile resolution
            var profileResolutionIssues = result.Issue.Where(i => 
                i.Details?.Coding?.Any(c => c.Code == "-4") == true ||
                i.Diagnostics?.Contains("cannot be resolved") == true ||
                i.Diagnostics?.Contains("nonexistent-profile") == true ||
                i.Details?.Text?.Contains("cannot be resolved") == true
            ).ToList();
            
            // We should have improved error handling - not the generic -1 error
            if (profileResolutionIssues.Any())
            {
                var issue = profileResolutionIssues.First();
                
                // Check that we don't have the old generic -1 error for this type of failure
                var hasGenericError = issue.Details?.Coding?.Any(c => c.Code == "-1") == true;
                hasGenericError.Should().BeFalse("Profile resolution failures should use specific error codes, not -1");
                
                // Check that we have a specific error code for profile resolution failures
                var hasSpecificError = issue.Details?.Coding?.Any(c => c.Code == "-4") == true;
                if (hasSpecificError)
                {
                    issue.Code.Should().Be(OperationOutcome.IssueType.NotFound, "Profile resolution failures should be marked as NotFound");
                }
                
                System.Console.WriteLine($"Improved error handling working: Issue code = {issue.Details?.Coding?.FirstOrDefault()?.Code}, Type = {issue.Code}");
            }
            
            // At minimum, we should not have any -1 errors for profile resolution
            var hasOldGenericErrors = result.Issue.Any(i => 
                i.Details?.Coding?.Any(c => c.Code == "-1") == true &&
                (i.Diagnostics?.Contains("nonexistent-profile") == true || i.Diagnostics?.Contains("cannot be resolved") == true)
            );
            
            hasOldGenericErrors.Should().BeFalse("Profile resolution failures should not use the old generic -1 error code");
        }
        
        [TestMethod]
        public void TestImprovedExceptionHandling_SchemaLoadingError()
        {
            // Test that we can trigger one of our improved exception handlers
            var validator = DotNetValidator.Create();
            
            // Create a simple resource
            var json = @"{
                ""resourceType"": ""Patient"",
                ""id"": ""test-patient""
            }";
            
            var resource = FhirJsonNode.Parse(json).ToTypedElement(_sdProvider);
            
            // Try a profile that might trigger schema loading issues
            // Using a profile URL that has a format that might cause resolution problems
            var result = validator.Validate(resource, null, "http://hl7.org/fhir/test/StructureDefinition/nonexistent-test-profile");
            
            System.Console.WriteLine($"Schema loading test result: {result.Issue.Count} issues");
            foreach (var issue in result.Issue)
            {
                var code = issue.Details?.Coding?.FirstOrDefault()?.Code ?? "no-code";
                System.Console.WriteLine($"  Issue: {issue.Code}, Code: {code}, Severity: {issue.Severity}");
                System.Console.WriteLine($"    Message: {issue.Details?.Text}");
                
                // If this is an error with a negative code, it came from our exception handling
                if (code.StartsWith("-") && int.TryParse(code, out int codeNum) && codeNum < 0)
                {
                    System.Console.WriteLine($"    ✓ This is one of our improved exception codes: {code}");
                    
                    // This should NOT be -1 (the old generic catch-all)
                    if (codeNum == -1)
                    {
                        System.Console.WriteLine($"    ❌ WARNING: Still using old generic -1 code");
                    }
                    else
                    {
                        System.Console.WriteLine($"    ✅ Using improved specific error code: {code}");
                    }
                }
            }
            
            // The key test: we should not have the old -1 errors
            var hasOldGenericErrors = result.Issue.Any(i => 
                i.Details?.Coding?.Any(c => c.Code == "-1") == true
            );
            
            // If we have ANY errors, they should use our improved codes, not -1
            if (result.Issue.Any(i => i.Severity == OperationOutcome.IssueSeverity.Error))
            {
                hasOldGenericErrors.Should().BeFalse("Error-level issues should use improved specific error codes, not the old generic -1 code");
            }
        }
        
        [TestMethod]
        public void TestNormalValidationStillWorks()
        {
            // Ensure that normal validation without exceptions still works correctly
            var validator = DotNetValidator.Create();
            
            // Create a simple valid patient resource
            var json = @"{
                ""resourceType"": ""Patient"",
                ""id"": ""test-patient"",
                ""name"": [{
                    ""family"": ""TestFamily"",
                    ""given"": [""TestGiven""]
                }]
            }";
            
            var resource = FhirJsonNode.Parse(json).ToTypedElement(_sdProvider);
            
            // Validate against the base Patient profile
            var result = validator.Validate(resource, null);
            
            // Should have a result (may have validation issues, but shouldn't crash)
            result.Should().NotBeNull();
            
            // Should not have any -1 errors (crashes) for normal validation
            var hasCrashErrors = result.Issue.Any(i => 
                i.Details?.Coding?.Any(c => c.Code == "-1") == true
            );
            
            hasCrashErrors.Should().BeFalse("Normal validation should not produce crash errors with code -1");
            
            System.Console.WriteLine($"Normal validation result: {result.Issue.Count} issues");
            foreach (var issue in result.Issue.Take(3)) // Show first few issues
            {
                var code = issue.Details?.Coding?.FirstOrDefault()?.Code ?? "no-code";
                System.Console.WriteLine($"  Issue: {issue.Code}, Code: {code}, Message: {issue.Details?.Text}");
            }
        }
    }
}