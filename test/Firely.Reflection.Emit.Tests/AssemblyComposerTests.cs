extern alias r4;
extern alias r4spec;
extern alias r5;
extern alias r5spec;
extern alias stu3;
extern alias stu3spec;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
    public class AssemblyComposerTests
    {
        public IAsyncResourceResolver ResolverSTU3;
        public IAsyncResourceResolver ResolverR4;
        public IAsyncResourceResolver ResolverR5;

        public AssemblyComposerTests()
        {
            static string path(string zip) => Path.Combine("speczips", zip);

            var zipSource3 = new stu3Spec.Source.ZipSource(path("specificationR3.zip"));
            ResolverSTU3 = new CachedResolver(zipSource3);

            var zipSource4 = new r4Spec.Source.ZipSource(path("specificationR4.zip"));
            ResolverR4 = new CachedResolver(zipSource4);

            var zipSource5 = new r5Spec.Source.ZipSource(path("specificationR5.zip"));
            ResolverR5 = new CachedResolver(zipSource5);
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
            var type = a.GetType("Patient")!;
            var ftas = (FhirTypeAttribute[])type.GetCustomAttributes(typeof(FhirTypeAttribute), false);
            Assert.IsTrue(ftas.Any(fta => fta.Name == "Patient" && fta.IsResource == true));

            type = a.GetType("Questionnaire")!;
            type = a.GetType("StructureDefinition")!;
        }

        [TestMethod]
        public async Task ComposeAssemblyAndLoadIt()
        {
            var systemTypePrefix = "http://hl7.org/fhirpath/System.";
            var composer = new AssemblyComposer("TestTypesAssembly", nameToCanonical, resolveToType, resolveToSourceNodeR4);

            var questionnaireSd = await composer.GetType("Questionnaire");
            var patientSd = await composer.GetType("Patient");
            var structureDefinitionSd = await composer.GetType("StructureDefinition");

            var tempPath = Path.GetTempPath();
            composer.WriteToDll(Path.Combine(tempPath, "TestTypesAssembly.dll"));

            byte[] rawAssembly = composer.GetAssemblyBytes();
            var ass = Assembly.Load(rawAssembly);
            Console.WriteLine(string.Join(", ", ass.GetTypes().Select(t => t.Name)));

            static string nameToCanonical(string name) => "http://hl7.org/fhir/StructureDefinition/" + name;

            Type? resolveToType(string canonical)
            {
                if (canonical.StartsWith(systemTypePrefix))
                {
                    var systemTypeName = canonical[systemTypePrefix.Length..];
                    if (P.Any.TryGetSystemTypeByName(systemTypeName, out Type? sys)) return sys;
                }

                return null;
            }
        }

        private async Task<ISourceNode?> resolveToSourceNodeR4(string canonical)
        {
            var sd = await ResolverR4.ResolveByCanonicalUriAsync(canonical);
            if (sd is null) return null;

            return r4Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

        private async Task<ISourceNode?> resolveToSourceNodeSTU3(string canonical)
        {
            var sd = await ResolverSTU3.ResolveByCanonicalUriAsync(canonical);
            if (sd is null) return null;

            return stu3Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

        private async Task<ISourceNode?> resolveToSourceNodeR5(string canonical)
        {
            var sd = await ResolverR5.ResolveByCanonicalUriAsync(canonical);
            if (sd is null) return null;

            return r5Core.ElementModel.TypedElementExtensions.ToTypedElement(sd).ToSourceNode();
        }

    }
}

