using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]

    public class CommonTypeRefComponentTests
    {
#if STU3
        [TestMethod]
        public void ConvertSTU3TypeRefs()
        {
            var code = new FhirUri("http://some.uri");
            code.AddExtension("http://example.com/extension", new FhirString("a string"));
            var list = new List<ElementDefinition.TypeRefComponent>
            {
                new ElementDefinition.TypeRefComponent
                {
                    CodeElement = code,
                    Profile = "profile1",
                    TargetProfile = "targetProfile1"
                },
                new ElementDefinition.TypeRefComponent
                {
                    Code = "http://some.uri",
                    Profile = "profile2",
                    TargetProfile = "targetProfile2"
                }
            };


            convertAndAssert(list, code);
        }
#else
        [TestMethod]
        public void ConvertR4TypeRefs()
        {
            var code = new FhirUri("http://some.uri");
            code.AddExtension("http://example.com/extension", new FhirString("a string"));
            var typeRefs = new ElementDefinition.TypeRefComponent
            {
                CodeElement = code,
                Profile = new[] { "profile1", "profile2" },
                TargetProfile = new[] { "targetProfile1", "targetProfile2" }
            };

            convertAndAssert(new[] { typeRefs }, code);
        }
#endif

        private void convertAndAssert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs, FhirUri exptectedCode)
        {
            var expected = new CommonTypeRefComponent
            {
                CodeElement = exptectedCode,
                Profile = new[] { "profile1", "profile2" },
                TargetProfile = new[] { "targetProfile1", "targetProfile2" }
            };

            var commonTypeRefs = CommonTypeRefComponent.Convert(typeRefs);
            var refType = commonTypeRefs.Should().ContainSingle().Subject;

            refType.Should().BeEquivalentTo(expected);
        }

    }

}
