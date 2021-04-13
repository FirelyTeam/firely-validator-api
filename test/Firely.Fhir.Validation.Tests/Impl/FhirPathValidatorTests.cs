/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Validation;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
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
        public async Task ValidateSuccess()
        {
            var validatable = new FhirPathValidator("test-1", "$this = 'test'", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var minimalContextWithFp = ValidationContext.BuildMinimalContext(fpCompiler: _fpCompiler);
            var result = await validatable.Validate(input, minimalContextWithFp).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Result.IsSuccessful, "the FhirPath Expression must be valid for this input");
        }

        [TestMethod]
        [ExpectedException(typeof(IncorrectElementDefinitionException), "A negative number was allowed.")]
        public void ValidateIncorrectFhirPath()
        {
            new FhirPathValidator("test -1", "this is not a fhirpath expression", "human description", IssueSeverity.Error, false);
        }

        [TestMethod]
        public async Task ValidateChildrenExists()
        {
            var humanName = ElementNodeAdapter.Root("HumanName");
            humanName.Add("family", "Brown", "string");
            humanName.Add("given", "Joe", "string");
            humanName.Add("given", "Patrick", "string");

            var validatable = new FhirPathValidator("test-1", "children().count() = 3", "human description", IssueSeverity.Error, false);

            var minimalContextWithFhirPath = ValidationContext.BuildMinimalContext(fpCompiler: _fpCompiler);

            var result = await validatable.Validate(humanName, minimalContextWithFhirPath).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Result.IsSuccessful, "the FhirPath Expression must not be valid for this input");
        }
    }
}
