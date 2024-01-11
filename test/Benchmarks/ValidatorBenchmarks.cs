using BenchmarkDotNet.Attributes;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
//using Validator = Hl7.Fhir.Validation.Validator;

namespace Firely.Sdk.Benchmarks
{
    [MemoryDiagnoser]
    public class ValidatorBenchmarks
    {
        private static readonly IResourceResolver ZIPSOURCE = new CachedResolver(new StructureDefinitionCorrectionsResolver(ZipSource.CreateValidationSource()));
        private static readonly IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);
        private static readonly string TEST_DIRECTORY = Path.GetFullPath(@"TestData\DocumentComposition");

        public ElementNode? TestResource = null;
        public string? InstanceTypeProfile = null;
        public IResourceResolver? TestResolver = null;

        [GlobalSetup]
        public void GlobalSetup()
        {
            //var testResourceData = File.ReadAllText(Path.Combine(TEST_DIRECTORY, "Levin.patient.xml"));
            var testResourceData = File.ReadAllText(Path.Combine(TEST_DIRECTORY, "MainBundle.bundle.xml"));

            TestResource = ElementNode.FromElement(FhirXmlNode.Parse(testResourceData).ToTypedElement(PROVIDER)!);
            //InstanceTypeProfile = Hl7.Fhir.Model.ModelInfo.CanonicalUriForFhirCoreType(TestResource.InstanceType).Value!;
            InstanceTypeProfile = "http://example.org/StructureDefinition/DocumentBundle";

            var testFilesResolver = new DirectorySource(TEST_DIRECTORY);
            TestResolver = new CachedResolver(new SnapshotSource(new CachedResolver(new MultiResolver(testFilesResolver, ZIPSOURCE))))!;

            // To avoid warnings about bi-model distributions, run the (slow) first-time run here in setup
            var cold = validateWip(TestResource!, InstanceTypeProfile!, TestResolver!);
            Debug.Assert(cold.Success);

            var oldCold = validateCurrent(TestResource!, InstanceTypeProfile!, TestResolver!);
            Debug.Assert(oldCold.Success);
        }

        [Benchmark]
        public void CurrentValidator()
        {
            _ = validateCurrent(TestResource!, InstanceTypeProfile!, TestResolver!);
        }

        [Benchmark]
        public void WipValidator()
        {
            _ = validateWip(TestResource!, InstanceTypeProfile!, TestResolver!);
        }

        private static Hl7.Fhir.Model.OperationOutcome validateWip(ElementNode typedElement, string schema, IResourceResolver rr)
        {
            var arr = rr.AsAsync();
            var ts = new LocalTerminologyService(arr);

            var validator = new Validator(arr, ts, new TestExternalReferenceResolver(rr));
            var result = validator.Validate(typedElement, schema);
            return result;
        }

        private static Hl7.Fhir.Model.OperationOutcome validateCurrent(ITypedElement typedElement, string profile, IResourceResolver arr)
        {
            // This code needs the new shims, and no longer compiles since the old validator has been removed from the SDK.
            throw new NotImplementedException();

            //var settings = new ValidationSettings
            //{
            //    GenerateSnapshot = true,
            //    GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
            //    ResourceResolver = arr,
            //    TerminologyService = new LocalTerminologyService(arr.AsAsync()),
            //    ResolveExternalReferences = true
            //};

            //var validator = new Validator(settings);
            //var outcome = profile is null ? validator.Validate(typedElement, Hl7.Fhir.Model.ModelInfo.ModelInspector) : validator.Validate(typedElement, Hl7.Fhir.Model.ModelInfo.ModelInspector, profile);
            //return outcome;
        }

        private class TestExternalReferenceResolver : IExternalReferenceResolver
        {
            public TestExternalReferenceResolver(IResourceResolver resolver)
            {
                Resolver = resolver;
            }

            public IResourceResolver Resolver { get; }

            public Task<object?> ResolveAsync(string reference) => Task.FromResult((object?)Resolver.ResolveByUri(reference));
        }
    }
}
