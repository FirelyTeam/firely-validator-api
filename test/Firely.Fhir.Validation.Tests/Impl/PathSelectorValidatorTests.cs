using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class PathSelectorValidatorTests
    {
        [TestMethod]
        public void StandardFPCompiler()
        {
            ValidationState state = new();
            state.Global.FPCompilerCache.Should().BeNull(because: "FhirPath cache should not be initialized yet");
            var context = ValidationSettings.BuildMinimalContext();
            var patient = ElementNodeAdapter.Root("Patient");
            var validator = new PathSelectorValidator("hasValue()", ResultAssertion.SUCCESS);

            validator.Validate(patient, context, state);

            state.Global.FPCompilerCache.Should().NotBeNull(because: "FhirPath cache should be initialized");
            state.Global.FPCompilerCache!.GetCompiledExpression("hasValue()").Should().NotBeNull(because: "symbol should be cached");
        }

        [TestMethod]
        public void UseOwnFPCompiler()
        {
            ValidationState state = new();
            state.Global.FPCompilerCache.Should().BeNull(because: "FhirPath cache should not be initialized yet");

            var symbols = new SymbolTable();
            symbols.Add("specialFunction", (ITypedElement f) => f);
            var compiler = new FhirPathCompiler(symbols);

            var context = ValidationSettings.BuildMinimalContext(null, null, compiler);

            var patient = ElementNodeAdapter.Root("Patient");

            var validator = new PathSelectorValidator("specialFunction()", ResultAssertion.SUCCESS);

            validator.Validate(patient, context, state);

            state.Global.FPCompilerCache.Should().NotBeNull(because: "FhirPath cache should be initialized");
            state.Global.FPCompilerCache!.GetCompiledExpression("specialFunction()").Should().NotBeNull(because: "symbol should be cached");
        }

        [TestMethod]
        public void UnknownSymbol()
        {
            ValidationState state = new();
            state.Global.FPCompilerCache.Should().BeNull(because: "FhirPath cache should not be initialized yet");

            var context = ValidationSettings.BuildMinimalContext();

            var patient = ElementNodeAdapter.Root("Patient");

            var validator = new PathSelectorValidator("unknownFunction()", ResultAssertion.SUCCESS);

            Action act = () => validator.Validate(patient, context, state);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Unknown symbol 'unknownFunction'");


            state.Global.FPCompilerCache.Should().NotBeNull(because: "FhirPath cache should be initialized");
        }
    }
}