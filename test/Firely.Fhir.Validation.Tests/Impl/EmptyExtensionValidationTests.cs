/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class EmptyExtensionValidationTests
    {
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