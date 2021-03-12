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
        public async Task TestExtractStructureDefinitionInfoFromSourceNode()
        {
            var sdNode = await resolveToSourceNode("http://hl7.org/fhir/StructureDefinition/Questionnaire");

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
        }

        [TestMethod]
        public async Task ComposeAssemblyWithPatientTypeAndDependants()
        {
            var systemTypePrefix = "http://hl7.org/fhirpath/System.";
            var composer = new AssemblyComposer("TestTypesAssembly", nameToCanonical, resolveToType, resolveToSourceNode);

            var questionnaireSd = await composer.GetType("Questionnaire");
            var patientSd = await composer.GetType("Patient");
            var structureDefinitionSd = await composer.GetType("StructureDefinition");

            var tempPath = Path.GetTempPath();
            composer.WriteToDll(Path.Combine(tempPath, "TestTypesAssembly.dll"));

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


        private async Task<ISourceNode?> resolveToSourceNode(string canonical)
        {
            var sd = await Resolver.ResolveByCanonicalUriAsync(canonical);
            if (sd is null) return null;

            return sd.ToTypedElement().ToSourceNode();
        }
    }
}

