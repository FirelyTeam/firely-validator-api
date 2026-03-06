using BenchmarkDotNet.Attributes;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.Specification.Source;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace Firely.Sdk.Benchmarks
{
    [MemoryDiagnoser]
    public class ResolverBenchmarks
    {
        private static readonly CachedResolver RESOLVER = new(ZipSource.CreateValidationSource());
        private static readonly StructureDefinitionCorrectionsResolver CORRECTING_RESOLVER = new(RESOLVER); // Not cached for benchmarking!

        [Params("integer", 
                "string", 
                "markdown", 
                "Age", 
                "Bundle", 
                "CareTeam", 
                "ElementDefinition", 
                "ImagingSelection", // R5 only!
                "OperationDefinition", 
                "Observation", 
                "Patient", 
                "Reference", 
                "StructureDefinition", 
                "Questionnaire")]
        public string Resource { get; set; } = null!;

        [Benchmark(Baseline = true)]
        public async Task ResolveWithoutCorrections()
        {
            await resolveAsync(RESOLVER, Resource);
        }

        [Benchmark]
        public async Task ResolveWithCorrections()
        {
            await resolveAsync(CORRECTING_RESOLVER, Resource);
        }

        private static async Task resolveAsync(IAsyncResourceResolver resolver, string resource)
        {
            var result = await resolver.ResolveByUriAsync($"http://hl7.org/fhir/StructureDefinition/{resource}");

            if (result == null)
                throw new KeyNotFoundException(resource);
        }
    }
}
