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
            var result = sliceValidator.Validate(new ElementNode[0], ValidationSettings.BuildMinimalContext(), new ValidationState());
            
            // Should fail because we have two mandatory slices that are not present
            result.IsSuccessful.Should().BeFalse();
            
            // Should contain cardinality errors for both purpose and valueSet
            var issues = result.Evidence.OfType<IssueAssertion>().ToList();
            issues.Should().HaveCount(2);
            issues.Should().Contain(i => i.Message.Contains("for slice purpose"));
            issues.Should().Contain(i => i.Message.Contains("for slice valueSet"));
        }
    }
}