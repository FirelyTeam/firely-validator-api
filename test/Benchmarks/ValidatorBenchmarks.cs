using BenchmarkDotNet.Attributes;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LegacyValidationSettings = Hl7.Fhir.Validation.ValidationSettings;
using LegacyValidator = Hl7.Fhir.Validation.Validator;
using Task = System.Threading.Tasks.Task;

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
        private Func<ITypedElement, string?, OperationOutcome> legacyValidator;
        private Func<ElementNode, string?, OperationOutcome> newValidator;
        private Func<ElementNode, string?, CancellationToken, Task<OperationOutcome>> newValidatorAsync;

        [Params(
            "MainBundle.bundle.xml",
            "Hippocrates.practitioner.xml",
            "Levin.patient.xml",
            "patient-clinicalTrial.xml",
            "Weight.observation.xml")]
        public string Resource { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var testResourceData = File.ReadAllText(Path.Combine(TEST_DIRECTORY, Resource));

            TestResource = ElementNode.FromElement(FhirXmlNode.Parse(testResourceData).ToTypedElement(PROVIDER)!);
            //InstanceTypeProfile = Hl7.Fhir.Model.ModelInfo.CanonicalUriForFhirCoreType(TestResource.InstanceType).Value!;
            InstanceTypeProfile = null;

            var testFilesResolver = new DirectorySource(TEST_DIRECTORY);
            TestResolver = new CachedResolver(new SnapshotSource(new CachedResolver(new MultiResolver(testFilesResolver, ZIPSOURCE))))!;

            var ts = new LocalTerminologyService(TestResolver.AsAsync());
            var settings = new LegacyValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = TestResolver,
                TerminologyService = ts,
                ResolveExternalReferences = true,
            };

            var lv = new LegacyValidator(settings);
            legacyValidator = (t, p) => string.IsNullOrEmpty(p) ? lv.Validate(t) : lv.Validate(t, p);

            var nv = new Validator(TestResolver.AsAsync(), ts, new TestExternalReferenceResolver(TestResolver));
            newValidator = nv.Validate;
            newValidatorAsync = nv.ValidateAsync;

            // To avoid warnings about bi-model distributions, run the (slow) first-time run here in setup
            var cold = newValidator(TestResource!, InstanceTypeProfile!);
            Debug.Assert(cold.Success);

            var oldCold = legacyValidator(TestResource!, InstanceTypeProfile!);
            Debug.Assert(oldCold.Success);
        }

        [Benchmark(Baseline = true)]
        public OperationOutcome LegacyValidator()
            => legacyValidator(TestResource!, InstanceTypeProfile!);

        [Benchmark]
        public OperationOutcome NewValidator()
            => newValidator(TestResource!, InstanceTypeProfile!);

        [Benchmark]
        public Task<OperationOutcome> NewValidatorAsync()
            => newValidatorAsync(TestResource!, InstanceTypeProfile!, default);

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
