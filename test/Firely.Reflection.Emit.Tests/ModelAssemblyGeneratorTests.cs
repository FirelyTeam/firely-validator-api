extern alias r4;
extern alias r4spec;
extern alias r5;
extern alias r5spec;
extern alias stu3;
extern alias stu3spec;
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
        public IAsyncResourceResolver ResolverSTU3;
        public IAsyncResourceResolver ResolverR4;
        public IAsyncResourceResolver ResolverR5;

        public ModelAssemblyGeneratorTests()
        {
            static string path(string zip) => Path.Combine("speczips", zip);

            var zipSource3 = new stu3Spec.Source.ZipSource(path("specificationR3.zip"));
            ResolverSTU3 = new CachedResolver(zipSource3);

            var zipSource4 = new r4Spec.Source.ZipSource(path("specificationR4.zip"));
            ResolverR4 = new CachedResolver(zipSource4);

            var zipSource5 = new r5Spec.Source.ZipSource(path("specificationR5.zip"));
            ResolverR5 = new CachedResolver(zipSource5);
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
            var genSTU3 = GetGenerator(resolveToSourceNodeSTU3);
            var genR4 = GetGenerator(resolveToSourceNodeR4);
            var genR5 = GetGenerator(resolveToSourceNodeR5);

            await test(genSTU3);
            await test(genR4);
            await test(genR5);

            static async Task test(ModelAssemblyGenerator generator)
            {
                await generator.AddType("Element");
                var testAssembly = generator.GetAssembly();

                // start at the bottom - Element
                var element = testAssembly.GetType("Element");
                Assert.AreEqual("Element", element!.Name);

                // The primitive type of the value element must be System.String
                var elementId = element.GetProperty("id");
                Assert.AreEqual(typeof(P.String), elementId!.PropertyType);

                // The id must be an attribute & not in summary
                var fhirElemA = elementId.GetCustomAttribute<FhirElementAttribute>();
                Assert.IsNotNull(fhirElemA, "No FhirElementAttribute generated");
                Assert.IsFalse(fhirElemA!.InSummary);
                Assert.AreEqual(XmlRepresentation.XmlAttr, fhirElemA.XmlSerialization);
                var cardElemA = elementId.GetCustomAttribute<CardinalityAttribute>();
                Assert.IsNull(cardElemA, "Should not have CardinalityAttribute");

                // check wether we created the repeating extension element correctly.
                // as a list with cardinality.
                var extensionElement = element.GetProperty("extension");
                Assert.IsTrue(typeof(ICollection).IsAssignableFrom(extensionElement!.PropertyType));
                var extension = testAssembly.GetType("Extension");
                Assert.AreEqual(extension, extensionElement.PropertyType.GetGenericArguments()[0]);
                var fhirElemB = elementId.GetCustomAttribute<FhirElementAttribute>();
                Assert.IsNotNull(fhirElemB, "No FhirElementAttribute generated");
                Assert.IsTrue(fhirElemB!.Order > fhirElemA.Order);
                var cardElemB = extensionElement.GetCustomAttribute<CardinalityAttribute>();
                Assert.IsNotNull(cardElemB, "No CardinalityAttribute generated");
                Assert.AreEqual(0, cardElemB!.Min);
                Assert.AreEqual(-1, cardElemB.Max);
            }
        }

        [TestMethod]
        public async Task TestExtractStructureDefinitionInfoFromSourceNode()
        {
            var sdNode = await resolveToSourceNodeR4("http://hl7.org/fhir/StructureDefinition/Questionnaire");

            var sd = StructureDefinitionInfo.FromStructureDefinition(sdNode!);
        }

        [TestMethod]
        public void CanLoadGeneratedAssembly()
        {
            var tempPath = Path.GetTempPath();
            var dllPath = Path.Combine(tempPath, "TestTypesAssembly.dll");
            var a = Assembly.LoadFile(dllPath);
            var type = a.GetType("Patient")!;    // typeof(Patient)   
            Console.WriteLine(String.Join(",", type.GetProperty("deceased")!.PropertyType.GetGenericArguments().Select(t => t.Name)));

            var ftas = (FhirTypeAttribute[])type.GetCustomAttributes(typeof(FhirTypeAttribute), false);
            Assert.IsTrue(ftas.Any(fta => fta.Name == "Patient" && fta.IsResource == true));

            type = a.GetType("Questionnaire")!;
            type = a.GetType("StructureDefinition")!;

            type = Type.GetType("Questionnaire");

            byte[] fileRaw = File.ReadAllBytes(dllPath);

            var ass = AppDomain.CurrentDomain.Load(fileRaw);
            var obj = AppDomain.CurrentDomain.CreateInstance(a.FullName!, "Patient");
            type = Type.GetType("Patient");

            var s = Stopwatch.StartNew();
            runit(a);
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds);

            s = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++) runit(a);
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds);

            static void runit(Assembly a)
            {
                string mn;
                string pn;
                {
                    foreach (var t in a.GetTypes())
                    {
                        foreach (var m in t.GetMembers())
                        {
                            if (m is MethodInfo)
                                mn = m.Name;
                            if (m is PropertyInfo pi)
                            {
                                var fte = pi.GetCustomAttribute<FhirElementAttribute>();
                                pn = fte!.Name;
                            }
                        }
                    }
                }
            }
        }

        const string SYSTEMTYPEPREFIX = "http://hl7.org/fhirpath/System.";

        private ModelAssemblyGenerator GetGenerator(Func<string, Task<ISourceNode?>> resolver)
        {
            return new ModelAssemblyGenerator("TestTypesAssembly", nameToCanonical, resolveToType, resolver);

            static string nameToCanonical(string name) => "http://hl7.org/fhir/StructureDefinition/" + name;

            Type? resolveToType(string canonical)
            {
                if (canonical.StartsWith(SYSTEMTYPEPREFIX))
                {
                    var systemTypeName = canonical[SYSTEMTYPEPREFIX.Length..];
                    if (P.Any.TryGetSystemTypeByName(systemTypeName, out Type? sys)) return sys;
                }

                return null;
            }
        }

        [TestMethod]
        public async Task ComposeAssemblyAndLoadIt()
        {
            var generator = GetGenerator(resolveToSourceNodeR4);
            await generator.AddType("Questionnaire");
            await generator.AddType("Patient");
            await generator.AddType("StructureDefinition");
            var testAssembly = generator.GetAssembly();

            var questionnaireSd = testAssembly.GetType("Questionnaire");
            var patientSd = testAssembly.GetType("Patient");
            var structureDefinitionSd = testAssembly.GetType("StructureDefinition");

            var tempPath = Path.GetTempPath();
            generator.WriteToDll(Path.Combine(tempPath, "TestTypesAssembly.dll"));

            byte[] rawAssembly = generator.GetAssemblyBytes();

            var ass = Assembly.Load(rawAssembly);
            Console.WriteLine(string.Join(", ", ass.GetTypes().Select(t => t.Name)));
        }

        private async Task<ISourceNode?> resolveToSourceNodeR4(string canonical)
        {
            var sd = await ResolverR4.ResolveByCanonicalUriAsync(canonical);
            return sd is null ? null : r4Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

        private async Task<ISourceNode?> resolveToSourceNodeSTU3(string canonical)
        {
            var sd = await ResolverSTU3.ResolveByCanonicalUriAsync(canonical);
            return sd is null ? null : stu3Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

        private async Task<ISourceNode?> resolveToSourceNodeR5(string canonical)
        {
            var sd = await ResolverR5.ResolveByCanonicalUriAsync(canonical);
            return sd is null ? null : r5Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

    }
}

