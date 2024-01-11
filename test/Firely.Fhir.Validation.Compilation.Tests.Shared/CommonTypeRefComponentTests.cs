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

#if STU3
        [TestMethod]
        [DynamicData("TestData")]
        public void CanConvertTest(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs, bool expected)
            => CommonTypeRefComponent.CanConvert(typeRefs).Should().Be(expected);

        public static IEnumerable<object[]> TestData =>
           new List<object[]>
           {
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new () { Code = "HumanName", Profile = "A", TargetProfile = "1"},
                    new () { Code = "HumanName", Profile = "B", TargetProfile = "1"} },
                    true
                },
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new () { Code = "HumanName", Profile = "A", TargetProfile = "1"},
                    new () { Code = "HumanName", Profile = "B", TargetProfile = "1"},
                    new () { Code = "HumanName", Profile = "A", TargetProfile = "2"},
                    new () { Code = "HumanName", Profile = "B", TargetProfile = "2"} },
                    true
                },
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new () { Code = "HumanName", Profile = "A", TargetProfile = "1"},
                    new (){ Code = "HumanName", Profile = "B"} },
                    false
                },
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new (){ Code = "HumanName", Profile = "A", TargetProfile = "1"},
                    new (){ Code = "HumanName", Profile = "A", TargetProfile = "2"},
                    new (){ Code = "HumanName", Profile = "B"} },
                    false
                },
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new () { Code = "HumanName", Profile = "A"},
                    new () { Code = "HumanName", Profile = "B"} },
                    true
                },
                new object[] { new ElementDefinition.TypeRefComponent[]
                {
                    new () { Code = "HumanName", TargetProfile = "1"},
                    new () { Code = "HumanName", TargetProfile = "2"} },
                    true
                },
           };
#endif
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

        private static void convertAndAssert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs, FhirUri exptectedCode)
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
