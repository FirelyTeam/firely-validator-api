﻿using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class FhirPathAssertionTests
    {
        private readonly FhirPathCompiler fpCompiler;

        public FhirPathAssertionTests()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            fpCompiler = new FhirPathCompiler(symbolTable);
        }

        [TestMethod]
        public void ValidateWithoutSettings()
        {
            var validatable = new FhirPathAssertion("test-1", "hasValue()", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var result = validatable.Validate(input, null);
        }

        [TestMethod]
        public async Task ValidateSuccess()
        {
            var validatable = new FhirPathAssertion("test-1", "$this = 'test'", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var result = await validatable.Validate(input, new ValidationContext() { FhirPathCompiler = fpCompiler }).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Result.IsSuccessful, "the FhirPath Expression must be valid for this input");
        }

        [TestMethod]
        [ExpectedException(typeof(IncorrectElementDefinitionException), "A negative number was allowed.")]
        public void ValidateIncorrectFhirPath()
        {
            _ = new FhirPathAssertion("test -1", "this is not a fhirpath expression", "human description", IssueSeverity.Error, false);
        }

        [TestMethod]
        public async Task ValidateChildrenExists()
        {
            var humanName = ElementNodeAdapter.Root("HumanName");
            humanName.Add("family", "Brown", "string");
            humanName.Add("given", "Joe", "string");
            humanName.Add("given", "Patrick", "string");

            var validatable = new FhirPathAssertion("test-1", "children().count() = 3", "human description", IssueSeverity.Error, false);

            var result = await validatable.Validate(humanName, new ValidationContext() { FhirPathCompiler = fpCompiler }).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Result.IsSuccessful, "the FhirPath Expression must not be valid for this input");
        }
    }
}
