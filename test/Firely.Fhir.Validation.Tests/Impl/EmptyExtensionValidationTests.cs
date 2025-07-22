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
            // and specifically that the fix for GitHub issue #544 works - paths ending with slice events
            // should properly extract slice information
            
            // Test 1: Path ending with slice event (the bug scenario that was fixed)
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
    }
}