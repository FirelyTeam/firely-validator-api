using BenchmarkDotNet.Attributes;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Firely.Sdk.Benchmarks
{
    [MemoryDiagnoser]
    public class ValidatorBenchmarks
    {
        private static readonly IResourceResolver ZIPSOURCE = new CachedResolver(ZipSource.CreateValidationSource());
        private static readonly IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);
        private static readonly string TEST_DIRECTORY = Path.GetFullPath(@"TestData\DocumentComposition");

        public ITypedElement? TestResource = null;
        public string? InstanceTypeProfile = null;
        public IElementSchemaResolver? SchemaResolver = null;
        public IResourceResolver? TestResolver = null;

        [GlobalSetup]
        public void GlobalSetup()
        {
            //var testResourceData = File.ReadAllText(Path.Combine(TEST_DIRECTORY, "Levin.patient.xml"));
            var testResourceData = File.ReadAllText(Path.Combine(TEST_DIRECTORY, "MainBundle.bundle.xml"));
            TestResource = FhirXmlNode.Parse(testResourceData).ToTypedElement(PROVIDER)!;
            //InstanceTypeProfile = Hl7.Fhir.Model.ModelInfo.CanonicalUriForFhirCoreType(TestResource.InstanceType).Value!;
            InstanceTypeProfile = "http://example.org/StructureDefinition/DocumentBundle";

            var testFilesResolver = new DirectorySource(TEST_DIRECTORY);
            TestResolver = new CachedResolver(new SnapshotSource(new CachedResolver(new MultiResolver(testFilesResolver, ZIPSOURCE))))!;
            SchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(TestResolver.AsAsync())!;

            // To avoid warnings about bi-model distributions, run the (slow) first-time run here in setup
            var cold = validateWip(TestResource!, InstanceTypeProfile!, TestResolver!, SchemaResolver!);
            Debug.Assert(cold.IsSuccessful);

            var oldCold = validateCurrent(TestResource!, InstanceTypeProfile!, TestResolver!, SchemaResolver!);
            Debug.Assert(oldCold.Success);
        }

        //[Benchmark]
        //public void CurrentValidator()
        //{
        //    _ = validateCurrent(TestResource!, InstanceTypeProfile!, TestResolver!, SchemaResolver!);
        //}

        [Benchmark]
        public void WipValidator()
        {
            _ = validateWip(TestResource!, InstanceTypeProfile!, TestResolver!, SchemaResolver!);
        }

        private ResultAssertion validateWip(ITypedElement typedElement, string profile, IResourceResolver arr, IElementSchemaResolver schemaResolver)
        {
            var schema = schemaResolver.GetSchema(profile);
            var constraintsToBeIgnored = new string[] { "rng-2", "dom-6" };
            var validationContext = new ValidationContext(schemaResolver,
                    new TerminologyServiceAdapter(new LocalTerminologyService(arr.AsAsync())))
            {
                ExternalReferenceResolver = u => Task.FromResult(arr.ResolveByUri(u)?.ToTypedElement()),
                // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                // should be removed from STU3/R4 once we get the new normative version
                // of FP up, which could do comparisons between quantities.
                // 2022-01-19 MS: added best practice constraint "dom-6" to be ignored, which checks if a resource has a narrative.
                ExcludeFilter = a => a is FhirPathValidator fhirPathAssertion && constraintsToBeIgnored.Contains(fhirPathAssertion.Key)
            };

            typedElement = ElementNode.FromElement(typedElement);
            var result = schema!.Validate(typedElement, validationContext);
            return result;
        }

        private Hl7.Fhir.Model.OperationOutcome validateCurrent(ITypedElement typedElement, string profile, IResourceResolver arr, IElementSchemaResolver schemaResolver)
        {
            var settings = new ValidationSettings
            {
                GenerateSnapshot = true,
                GenerateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault(),
                ResourceResolver = arr,
                TerminologyService = new LocalTerminologyService(arr.AsAsync()),
                ResolveExternalReferences = true
            };

            var validator = new Validator(settings);
            var outcome = profile is null ? validator.Validate(typedElement) : validator.Validate(typedElement, profile);
            return outcome;
        }

    }
}
