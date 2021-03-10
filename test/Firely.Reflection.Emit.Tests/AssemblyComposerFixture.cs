using Hl7.Fhir.Specification.Source;

namespace Firely.Reflection.Emit.Tests
{
    public class AssemblyComposerFixture
    {
        public IAsyncResourceResolver Resolver;

        public AssemblyComposerFixture()
        {
            var zipSource = ZipSource.CreateValidationSource();
            Resolver = new CachedResolver(zipSource);
        }
    }
}

