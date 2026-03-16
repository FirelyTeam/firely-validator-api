using BenchmarkDotNet.Attributes;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System.IO;

namespace Firely.Sdk.Benchmarks;

[CrossVersionConfiguration(typeof(ValidatorBenchmarks))]
[PackageVersion("2.8.0-alpha-20250905.1", "6.0.0-rc2-20250915.4", Constant = "VALSDK6")]
[PackageVersion("2.7.0", "5.12.1", Constant = "VALSDK5", Baseline = true)]
// [PackageVersion("2.2.0")]
// [ProjectReference]
public class ValidatorBenchmarks
{
    private static readonly IAsyncResourceResolver ZIPSOURCE = new CachedResolver(new StructureDefinitionCorrectionsResolver(ZipSource.CreateValidationSource()));
    private static readonly IStructureDefinitionSummaryProvider PROVIDER = new StructureDefinitionSummaryProvider(ZIPSOURCE);
    private static readonly string TEST_DIRECTORY = Path.GetFullPath(@"TestData");
    private static readonly string TEST_BUNDLE_PATH = Path.Combine(Path.GetFullPath(@"TestData"), "ValidationBundle.json");
    
    private Validator Validator { get; set; } = null!;
    
#if VALSDK6
    private PocoNode TestResource { get; set; } = null!;
#elif VALSDK5
    private ElementNode TestResource { get; set; } = null!;
#endif
    
    [GlobalSetup]
    public void Setup()
    {
        var testResourceData = File.ReadAllText(TEST_BUNDLE_PATH);
        
#if VALSDK6
        var resource = FhirJsonDeserializer.OSTRICH.DeserializeResource(testResourceData);
        TestResource = resource.ToPocoNode();
#elif VALSDK5
        var resource = FhirJsonNode.Parse(testResourceData).ToTypedElement(ModelInfo.ModelInspector);
        TestResource = ElementNode.FromElement(resource);
#endif
        
        var ts = new LocalTerminologyService(ZIPSOURCE);

        Validator = new Validator(ZIPSOURCE, ts);
    }

    [Benchmark]
    public OperationOutcome PerformanceLargeValidation()
    {
        return Validator.Validate(TestResource);
    }
}