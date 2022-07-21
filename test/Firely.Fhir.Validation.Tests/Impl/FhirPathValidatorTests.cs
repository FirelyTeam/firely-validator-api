/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class FhirPathValidatorTests
    {
        private readonly FhirPathCompiler _fpCompiler;

        public FhirPathValidatorTests()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            _fpCompiler = new FhirPathCompiler(symbolTable);
        }

        [TestMethod]
        public void ValidateWithoutSettings()
        {
            var validatable = new FhirPathValidator("test-1", "hasValue()", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");
            _ = validatable.Validate(input, ValidationContext.BuildMinimalContext());
        }



        [TestMethod]
        public void ValidateSuccess()
        {
            var validatable = new FhirPathValidator("test-1", "$this = 'test'", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var minimalContextWithFp = ValidationContext.BuildMinimalContext(fpCompiler: _fpCompiler);
            var result = validatable.Validate(input, minimalContextWithFp);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessful, "the FhirPath Expression must be valid for this input");
        }

        [TestMethod]
        public void ValidateIncorrectFhirPath()
        {
            var validator = new FhirPathValidator("test -1", "this is not a fhirpath expression", "human description", IssueSeverity.Error, false);
            var data = ElementNode.ForPrimitive("hi!");

            // Run-time error
            var result = validator.Validate(data, ValidationContext.BuildMinimalContext());
            result.Evidence.OfType<IssueAssertion>().Single().IssueNumber.Should().Be(Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION.Code);

        }

        [TestMethod]
        public void ValidateChildrenExists()
        {
            var humanName = ElementNodeAdapter.Root("HumanName");
            humanName.Add("family", "Brown", "string");
            humanName.Add("given", "Joe", "string");
            humanName.Add("given", "Patrick", "string");

            var validatable = new FhirPathValidator("test-1", "children().count() = 3", "human description", IssueSeverity.Error, false);

            var minimalContextWithFhirPath = ValidationContext.BuildMinimalContext(fpCompiler: _fpCompiler);

            var result = validatable.Validate(humanName, minimalContextWithFhirPath);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessful, "the FhirPath Expression must not be valid for this input");
        }
    }
}
