/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;
using Task = System.Threading.Tasks.Task;

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
            symbolTable.AddFhirExtensions();
            _fpCompiler = new FhirPathCompiler(symbolTable);
        }

        [TestMethod]
        public void ValidateWithoutSettings()
        {
            var validatable = new FhirPathValidator("test-1", "hasValue()", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");
            _ = validatable.Validate(input, ValidationSettings.BuildMinimalContext());
        }



        [TestMethod]
        public void ValidateSuccess()
        {
            var validatable = new FhirPathValidator("test-1", "$this = 'test'", "human description", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var minimalContextWithFp = ValidationSettings.BuildMinimalContext(fpCompiler: _fpCompiler);
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
            var result = validator.Validate(data, ValidationSettings.BuildMinimalContext());
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

            var minimalContextWithFhirPath = ValidationSettings.BuildMinimalContext(fpCompiler: _fpCompiler);

            var result = validatable.Validate(humanName, minimalContextWithFhirPath);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessful, "the FhirPath Expression must not be valid for this input");
        }

        [TestMethod]
        public void ValidateNullableInput()
        {
            var validatable = new FhirPathValidator("test-1", "{}", "This expression results in empty", IssueSeverity.Error, false);

            var input = ElementNode.ForPrimitive("test");

            var minimalContextWithFp = ValidationSettings.BuildMinimalContext(fpCompiler: _fpCompiler);
            var result = validatable.Validate(input, minimalContextWithFp);

            result?.IsSuccessful.Should().Be(false, because: "FhirPath epressions resulting in null should return false in the FhirPathValidator");
        }

        [TestMethod]
        public void ValidateMemberOf()
        {
            var validatable = new FhirPathValidator("memberof", "memberOf('http://hl7.org/fhir/ValueSet/iso3166-1-2')", "This expression should use FP memberOf()", IssueSeverity.Error, false);

            var input = ElementNodeAdapter.Root("Coding");
            input.Add("system", "http://hl7.org/fhir/ValueSet/iso3166-1-2", "uri");
            input.Add("code", "NL", "string");

            var minimalContextWithFp = ValidationSettings.BuildMinimalContext(fpCompiler: _fpCompiler);
            minimalContextWithFp.ValidateCodeService = new AlwaysSuccessfulTS();
            var result = validatable.Validate(input, minimalContextWithFp);

            result?.Warnings.Should().BeEmpty(because: "There should be no warnings about the terminology service.");
            result?.IsSuccessful.Should().BeTrue();
        }

        /// <summary>
        /// A CodeValidationTerminologyService that always return a success with the method <see cref="ValueSetValidateCode"/>. Only meant for
        /// testing purposes.
        /// </summary>
        private class AlwaysSuccessfulTS : ICodeValidationTerminologyService
        {
            public Task<Parameters> Subsumes(Parameters parameters, string? id = null, bool useGet = false) =>
                throw new System.NotImplementedException();

            public Task<Parameters> ValueSetValidateCode(Parameters parameters, string? id = null, bool useGet = false) =>
                Task.FromResult(new Parameters
                {
                    Parameter = new List<Parameters.ParameterComponent>() {
                        new Parameters.ParameterComponent()
                        {
                            Name = "result",
                            Value = new FhirBoolean(true)}
                    }
                });
        }
    }
}
