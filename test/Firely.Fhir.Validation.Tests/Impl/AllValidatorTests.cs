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
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class AllValidatorTests
    {

        private abstract class ResultAssertion : IValidatable
        {
            private readonly string _message;
            private readonly ValidationResult _result;

            public ResultAssertion(string message, ValidationResult result) { _message = message; _result = result; }

            public JToken ToJson()
            {
                throw new System.NotImplementedException();
            }

            public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
            {
                return
                    new ResultReport(_result, new TraceAssertion(state.Location.InstanceLocation.ToString(), _message));
            }
        }

        private class SuccessAssertion : ResultAssertion
        {
            public SuccessAssertion(string message = "Success Assertion") : base(message, ValidationResult.Success) { }
        }

        private class FailureAssertion : ResultAssertion
        {
            public FailureAssertion(string message = "Failure Assertion") : base(message, ValidationResult.Failure) { }

        }


        [TestMethod]
        public void SingleOperand()
        {
            var allAssertion = new AllValidator(new SuccessAssertion());
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationSettings.BuildMinimalContext());
            Assert.IsTrue(result.IsSuccessful);

            allAssertion = new AllValidator(new FailureAssertion());
            result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationSettings.BuildMinimalContext());
            Assert.IsFalse(result.IsSuccessful);
        }

        [TestMethod]
        public void Combinations()
        {
            var allAssertion = new AllValidator(new SuccessAssertion(), new FailureAssertion());
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationSettings.BuildMinimalContext());
            Assert.IsFalse(result.IsSuccessful);

        }

        [TestMethod]
        public void ShortcircuitEvaluationTest()
        {
            var assertions = new IAssertion[] { new SuccessAssertion("S1"),
                                                new SuccessAssertion("S2"),
                                                new FailureAssertion("F1"),
                                                new FailureAssertion("F2"),
                                                new SuccessAssertion("S3"),
                                                new FailureAssertion("F3")};

            var allAssertion = new AllValidator(shortcircuitEvaluation: true, assertions);
            var result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationSettings.BuildMinimalContext());
            result.IsSuccessful.Should().Be(false);
            result.Evidence.OfType<TraceAssertion>().Select(t => t.Message)
                           .Should()
                           .BeEquivalentTo("S1", "S2", "F1");

            allAssertion = new AllValidator(shortcircuitEvaluation: false, assertions);
            result = allAssertion.Validate(ElementNode.ForPrimitive(1), ValidationSettings.BuildMinimalContext());
            result.IsSuccessful.Should().Be(false);
            result.Evidence.OfType<TraceAssertion>().Select(t => t.Message)
                           .Should()
                           .BeEquivalentTo("S1", "S2", "F1", "F2", "S3", "F3");
        }
    }
}
