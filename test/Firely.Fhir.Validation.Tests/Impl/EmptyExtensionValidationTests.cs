/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class EmptyExtensionValidationTests
    {
        [TestMethod]
        public void EmptyExtensionShouldFailCardinalityValidation()
        {
            // This test verifies that empty extensions fail cardinality validation for required sub-elements
            var validator = new CardinalityValidator(1, 1);
            var result = validator.Validate(new ElementNode[0], ValidationSettings.BuildMinimalContext(), new ValidationState());
            
            // Empty collection should fail cardinality 1..1
            result.IsSuccessful.Should().BeFalse();
        }

        [TestMethod]
        public void SliceValidatorShouldValidateEmptySlices()
        {
            // Test that slice validation works for empty collections - reproduces the issue pattern
            var purposeSlice = new SliceValidator.SliceCase(
                "purpose", 
                new FixedValidator(ElementNode.ForPrimitive("purpose")),
                new CardinalityValidator(1, 1)
            );
            
            var valueSetSlice = new SliceValidator.SliceCase(
                "valueSet",
                new FixedValidator(ElementNode.ForPrimitive("valueSet")), 
                new CardinalityValidator(1, 1)
            );

            var sliceValidator = new SliceValidator(
                ordered: false, 
                defaultAtEnd: false, 
                @default: new CardinalityValidator(0, 0), // Default should have no elements
                purposeSlice, valueSetSlice
            );

            // Empty input - should fail because purpose and valueSet slices are required (1..1)
            var settings = ValidationSettings.BuildMinimalContext();
            var result = sliceValidator.Validate(new ElementNode[0], settings, new ValidationState());
            
            // Apply CleanUp to add slice context to error messages
            result = result.CleanUp();
            
            // Should fail because we have two mandatory slices that are not present
            result.IsSuccessful.Should().BeFalse();
            
            // Should contain cardinality errors for both purpose and valueSet
            var issues = result.Evidence.OfType<IssueAssertion>().ToList();
            issues.Should().HaveCount(2);
            issues.Should().Contain(i => i.Message.Contains("for slice purpose"));
            issues.Should().Contain(i => i.Message.Contains("for slice valueSet"));
        }

        [TestMethod]
        public void ValidateSliceInfoExtractionForVariousPathEndingScenarios()
        {
            // This test validates that TryGetSliceInfo works correctly for various path scenarios
            
            // Test 1: Path ending with slice event (the bug scenario)
            var pathEndingWithSlice = DefinitionPath.Start().CheckSlice("purpose", "string");
            pathEndingWithSlice.TryGetSliceInfo(out var sliceInfo).Should().BeTrue();
            sliceInfo.Should().Be("purpose");
            
            // Test 2: Path with slice followed by child (the working scenario)
            var pathWithSliceAndChild = DefinitionPath.Start()
                .CheckSlice("purpose", "string")
                .ToChild("value", "string");
            pathWithSliceAndChild.TryGetSliceInfo(out sliceInfo).Should().BeTrue();
            sliceInfo.Should().Be("purpose");
            
            // Test 3: Path with multiple slices (nested scenario)
            var pathWithMultipleSlices = DefinitionPath.Start()
                .CheckSlice("outer", "string")
                .CheckSlice("inner", "string");
            pathWithMultipleSlices.TryGetSliceInfo(out sliceInfo).Should().BeTrue();
            sliceInfo.Should().Be("outer, subslice inner");
            
            // Test 4: Path with no slices
            var pathWithoutSlices = DefinitionPath.Start().ToChild("name", "string");
            pathWithoutSlices.TryGetSliceInfo(out sliceInfo).Should().BeFalse();
            sliceInfo.Should().BeNull();
        }

        [TestMethod]
        public void EmptySliceValidationShouldReportSliceNamesFromRobsSuggestedTest()
        {
            // This test implements Rob's suggested test case from GitHub issue #544
            // It ensures that when slice validation fails, the error messages include slice names
            // This addresses the core bug where paths ending with slice events didn't extract slice info
            
            // Create a slice validator with two mandatory slices (mimicking additional-binding extension)
            var purposeSlice = new SliceValidator.SliceCase(
                "purpose", 
                new FixedValidator(ElementNode.ForPrimitive("purpose")),
                new CardinalityValidator(1, 1) // Required
            );
            
            var valueSetSlice = new SliceValidator.SliceCase(
                "valueSet",
                new FixedValidator(ElementNode.ForPrimitive("valueSet")), 
                new CardinalityValidator(1, 1) // Required
            );

            var sliceValidator = new SliceValidator(
                ordered: false, 
                defaultAtEnd: false, 
                @default: new CardinalityValidator(0, null), // Allow additional elements
                purposeSlice, valueSetSlice
            );

            // Test empty collection - should report both missing mandatory slices with slice names
            var settings = ValidationSettings.BuildMinimalContext();
            var result = sliceValidator.Validate(new ElementNode[0], settings, new ValidationState());
            result = result.CleanUp(); // This step adds slice context to error messages

            result.IsSuccessful.Should().BeFalse();
            var issues = result.Evidence.OfType<IssueAssertion>().ToList();
            
            // Should report exactly 2 issues - one for each missing mandatory slice
            issues.Should().HaveCount(2);
            
            // Each issue should contain the slice name in the message (this is the bug fix)
            issues.Should().Contain(i => i.Message.Contains("for slice purpose"), 
                "Should report missing purpose slice with slice name in message");
            issues.Should().Contain(i => i.Message.Contains("for slice valueSet"),
                "Should report missing valueSet slice with slice name in message");
            
            // Before the fix, these messages would not contain "for slice purpose" or "for slice valueSet"
            // because TryGetSliceInfo would return false for paths ending with slice events
        }

        [TestMethod]
        public void EmptyAdditionalBindingExtensionShouldReportMissingSlices()
        {
            // This test simulates the exact issue described in the GitHub issue:
            // An empty additional-binding extension should report missing mandatory slices
            
            // Simulate an extension with required slices but no elements provided
            var purposeSlice = new SliceValidator.SliceCase(
                "purpose", 
                new FixedValidator(ElementNode.ForPrimitive("purpose")),
                new CardinalityValidator(1, 1) // Required
            );
            
            var valueSetSlice = new SliceValidator.SliceCase(
                "valueSet",
                new FixedValidator(ElementNode.ForPrimitive("valueSet")), 
                new CardinalityValidator(1, 1) // Required
            );

            var sliceValidator = new SliceValidator(
                ordered: false, 
                defaultAtEnd: false, 
                @default: new CardinalityValidator(0, null), // Allow additional elements
                purposeSlice, valueSetSlice
            );

            // Test empty extension - should report both missing mandatory slices
            var settings = ValidationSettings.BuildMinimalContext();
            var result = sliceValidator.Validate(new ElementNode[0], settings, new ValidationState());
            result = result.CleanUp();
            
            result.IsSuccessful.Should().BeFalse();
            var issues = result.Evidence.OfType<IssueAssertion>().ToList();
            
            // Should report exactly 2 issues - one for each missing mandatory slice
            issues.Should().HaveCount(2);
            issues.Should().Contain(i => i.Message.Contains("for slice purpose"));
            issues.Should().Contain(i => i.Message.Contains("for slice valueSet"));
            
            // Test non-empty extension with some elements but missing mandatory slices
            var nonEmptyElements = new[] { ElementNode.ForPrimitive("documentation") };
            result = sliceValidator.Validate(nonEmptyElements, settings, new ValidationState());
            result = result.CleanUp();
            
            result.IsSuccessful.Should().BeFalse();
            issues = result.Evidence.OfType<IssueAssertion>().ToList();
            
            // Should still report the 2 missing mandatory slices
            issues.Should().Contain(i => i.Message.Contains("for slice purpose"));
            issues.Should().Contain(i => i.Message.Contains("for slice valueSet"));
        }
    }
}