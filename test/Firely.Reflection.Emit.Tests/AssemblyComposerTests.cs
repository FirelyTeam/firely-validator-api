using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Specification.Source;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Firely.Reflection.Emit.Tests
{
    [TestClass]
    public class AssemblyComposerTests
    {
        public IAsyncResourceResolver Resolver;

        public AssemblyComposerTests()
        {
            var zipSource = ZipSource.CreateValidationSource();
            Resolver = new CachedResolver(zipSource);
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
        }

        [TestMethod]
        public async Task ComposeAssemblyWithPatientTypeAndDependants()
        {
            var composer = new AssemblyComposer("TestTypesAssembly", nameToCanonical, resolveCanonical);

            var patientSd = await composer.GetTypeByName("Patient");
            var structureDefinitionSd = await composer.GetTypeByName("StructureDefinition");

            var tempPath = Path.GetTempPath();
            composer.WriteToDll(Path.Combine(tempPath, "TestTypesAssembly.dll"));

            static string nameToCanonical(string name) => "http://hl7.org/fhir/StructureDefinition/" + name;

            async Task<ISourceNode> resolveCanonical(string can) => await getSdFor(can);
        }


        private async Task<ISourceNode> getSdFor(string canonical)
        {
            var sd = await Resolver.ResolveByCanonicalUriAsync(canonical);
            return sd.ToTypedElement().ToSourceNode();
        }
    }
}

