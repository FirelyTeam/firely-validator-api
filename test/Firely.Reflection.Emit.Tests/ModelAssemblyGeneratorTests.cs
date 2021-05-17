extern alias r4;
extern alias r4spec;
extern alias r5;
extern alias r5spec;
extern alias stu3;
extern alias stu3spec;

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Sdk;
using P = Hl7.Fhir.ElementModel.Types;
using r4Core = r4.Hl7.Fhir;
using r4Spec = r4spec.Hl7.Fhir.Specification;
using r5Core = r5.Hl7.Fhir;
using r5Spec = r5spec.Hl7.Fhir.Specification;
using stu3Core = stu3.Hl7.Fhir;
using stu3Spec = stu3spec.Hl7.Fhir.Specification;

namespace Firely.Reflection.Emit.Tests
{
    [TestClass]
    public class ModelAssemblyGeneratorTests
    {
        private const string STUBSNS = "Firely.Fhir.Model.Stubs";
        public IAsyncResourceResolver ResolverSTU3;
        public IAsyncResourceResolver ResolverR4;
        public IAsyncResourceResolver ResolverR5;

        public ModelAssemblyGeneratorTests()
        {
            static string path(string zip) => Path.Combine("speczips", zip);

            var zipSource3 = new stu3Spec.Source.ZipSource(path("specificationSTU3.zip"));
            ResolverSTU3 = new CachedResolver(zipSource3);

            var zipSource4 = new r4Spec.Source.ZipSource(path("specificationR4.zip"));
            ResolverR4 = new CachedResolver(zipSource4);

            var zipSource5 = new r5Spec.Source.ZipSource(path("specificationR5.zip"));
            ResolverR5 = new CachedResolver(zipSource5);
        }

        [TestMethod, Ignore]
        public void UseMyGeneratedTypes()
        {
            var t = typeof(Firely.Fhir.Model.Stubs.Questionnaire_EnableWhen);
            var pt = t.GetProperty("answer")!.PropertyType;
            if (pt.Namespace == "Unions" && pt.Name.StartsWith("UnionType_"))
            {
                var types = string.Join(", ", pt.GetGenericArguments().Select(a => a.Name));
                Debug.WriteLine(types);
            }

            var extensionType = typeof(Firely.Fhir.Model.Stubs.Extension);
            var valueProp = extensionType!.GetProperty("value");
            var elementAttr = valueProp!.GetCustomAttribute<FhirElementAttribute>();
            elementAttr!.Choice.Should().Be(ChoiceType.DatatypeChoice);
        }

        // test contained
        // test base classes
        // test union
        // test narrative
        // test cases from webpage
        // test cases from ISDSP tests
        // test backbone (Questionnaire.item.item.item)
        // InSummary

        [TestMethod]
        public async Task TestGeneratePrimitives()
        {
            await testAllReleases(test);

            static async Task test(ModelAssemblyGenerator generator, FhirRelease _)
            {
                await generator.AddType("Element");
                var testAssembly = generator.GetAssembly();

                // start at the bottom - Element
                var element = testAssembly.GetType($"{STUBSNS}.Element");
                Assert.AreEqual("Element", element!.Name, "The name of the generated type should be the FHIR type name");

                // The primitive type of the value element must be System.String
                var elementId = element.GetProperty("id");
                Assert.AreEqual(typeof(P.String), elementId!.PropertyType, "the primitive Element.id property should be a System.String");

                // The id must be an attribute & not in summary
                var fhirElemA = elementId.GetCustomAttribute<FhirElementAttribute>();
                Assert.IsNotNull(fhirElemA, "Element.Id should have a FhirElementAttribute");
                Assert.IsFalse(fhirElemA!.InSummary, "Element.Id is not InSummary");
                Assert.AreEqual(XmlRepresentation.XmlAttr, fhirElemA.XmlSerialization, "Element.Id should be serialized as an attribute");
                var cardElemA = elementId.GetCustomAttribute<CardinalityAttribute>();
                Assert.IsNull(cardElemA, "Element.Id is not a collection so should not have CardinalityAttribute");

                // check wether we created the repeating extension element correctly as a list with cardinality.
                var extensionElement = element.GetProperty("extension");
                Assert.IsTrue(typeof(ICollection).IsAssignableFrom(extensionElement!.PropertyType), "extension element should be a collection");
                Assert.AreEqual(testAssembly.GetType($"{STUBSNS}.Extension"), extensionElement.PropertyType.GetGenericArguments()[0], "extension element should be of type Extension");
                var fhirElemB = extensionElement.GetCustomAttribute<FhirElementAttribute>();
                Assert.IsNotNull(fhirElemB, "extension element should have a FhirElementAttribute");
                Assert.IsTrue(fhirElemB!.Order > fhirElemA.Order);
                var cardElemB = extensionElement.GetCustomAttribute<CardinalityAttribute>();
                Assert.IsNotNull(cardElemB, "extension element shouldhave a CardinalityAttribute");
                Assert.AreEqual(0, cardElemB!.Min);
                Assert.AreEqual(-1, cardElemB.Max);

            }
        }

        [TestMethod]
        public async Task TestGenerateFhirTypeAttribute()
        {
            await testAllReleases(test);

            static async Task test(ModelAssemblyGenerator generator, FhirRelease _)
            {
                await generator.AddType("Questionnaire");
                var testAssembly = generator.GetAssembly();

                testTypeAttribute("Questionnaire", true, false);
                testTypeAttribute("Questionnaire_Item", false, true);
                testTypeAttribute("boolean", false, false);

                void testTypeAttribute(string typeName, bool isResource, bool isNested)
                {
                    var q = testAssembly.GetType($"{STUBSNS}.{typeName}");
                    var ta = q!.GetCustomAttribute<FhirTypeAttribute>();
                    ta!.Name.Should().Be(typeName);
                    ta.IsResource.Should().Be(isResource);
                    ta.IsNestedType.Should().Be(isNested);
                }
            }
        }

        [TestMethod]
        public async Task ComposeAssemblyAndLoadIt()
        {
            await testAllReleases(test);

            static async Task test(ModelAssemblyGenerator generator, FhirRelease release)
            {
                await generator.AddType("Questionnaire");
                await generator.AddType("Patient");
                await generator.AddType("StructureDefinition");
                await generator.AddType("Money");
                await generator.AddType("Distance");
                var testAssembly = generator.GetAssembly();
                tryGetSomeTypes(testAssembly);

                // Write the dynamically generated DLL out to a file in temp
                // so we can inspect it with ILdasm & load it.
                var output = generator.WriteToDll(new DirectoryInfo(Path.GetTempPath()));
                Debug.WriteLine("Created dll " + testAssembly.GetName().Name);

                testAssembly = Assembly.LoadFile(output);
                tryGetSomeTypes(testAssembly);

                // Write to a byte array and load it from the array.
                byte[] rawAssembly = generator.GetAssemblyBytes();
                var ass = Assembly.Load(rawAssembly);
                tryGetSomeTypes(testAssembly);

                static void tryGetSomeTypes(Assembly a)
                {
                    Assert.IsNotNull(a.GetType($"{STUBSNS}.Questionnaire"));
                    Assert.IsNotNull(a.GetType($"{STUBSNS}.Patient"));
                    Assert.IsNotNull(a.GetType($"{STUBSNS}.StructureDefinition"));

                    var extensionType = a.GetType($"{STUBSNS}.Extension");
                    var valueProp = extensionType!.GetProperty("value");
                    var elementAttr = valueProp!.GetCustomAttribute<FhirElementAttribute>();
                    elementAttr!.Choice.Should().Be(ChoiceType.DatatypeChoice);
                    elementAttr.XmlSerialization.Should().Be(XmlRepresentation.XmlElement);

                    var elementType = a.GetType($"{STUBSNS}.Element");
                    var idProp = elementType!.GetProperty("id");
                    elementAttr = idProp!.GetCustomAttribute<FhirElementAttribute>();
                    elementAttr!.Choice.Should().Be(ChoiceType.None);
                    elementAttr.XmlSerialization.Should().Be(XmlRepresentation.XmlAttr);
                }
            }
        }

        private async Task testAllReleases(Func<ModelAssemblyGenerator, FhirRelease, Task> test)
        {
            var genSTU3 = buildGenerator("stu3", resolveToSourceNodeSTU3);
            var genR4 = buildGenerator("r4", resolveToSourceNodeR4);
            var genR5 = buildGenerator("r5", resolveToSourceNodeR5);

            await test(genSTU3, FhirRelease.STU3);
            await test(genR4, FhirRelease.R4);
            await test(genR5, FhirRelease.R5);

            async Task<ISourceNode?> resolveToSourceNodeR4(string canonical)
            {
                var sd = await ResolverR4.ResolveByCanonicalUriAsync(canonical);
                return sd is null ? null : r4Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
            }

            async Task<ISourceNode?> resolveToSourceNodeSTU3(string canonical)
            {
                var sd = await ResolverSTU3.ResolveByCanonicalUriAsync(canonical);
                return sd is null ? null : stu3Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
            }

            async Task<ISourceNode?> resolveToSourceNodeR5(string canonical)
            {
                var sd = await ResolverR5.ResolveByCanonicalUriAsync(canonical);
                return sd is null ? null : r5Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
            }

            ModelAssemblyGenerator buildGenerator(string release, Func<string, Task<ISourceNode?>> resolver) =>
                new($"TestTypesAssembly-{release}", STUBSNS, resolver);
        }

    }
}

