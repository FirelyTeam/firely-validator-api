using Firely.Fhir.Validation;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;

namespace Firely.Validation.Compilation.Tests
{
    public class SchemaConverterFixture
    {
        public readonly IElementSchemaResolver SchemaResolver;
        public readonly FhirPathCompiler FpCompiler;
        public readonly ITerminologyServiceNEW TerminologyService;
        public readonly IAsyncResourceResolver ResourceResolver;
        public readonly SchemaConverter Converter;

        public SchemaConverterFixture()
        {
            ResourceResolver = new CachedResolver(
                new SnapshotSource(new MultiResolver(
                    //enable this to be able to dump test profiles in your %TEMP/testprofiles
                    //directory to debug the generator.
                    //new DirectorySource(Path.Combine(Path.GetTempPath(), "testprofiles")),
                    new TestProfileArtifactSource(),
                    ZipSource.CreateValidationSource())));

            SchemaResolver = new StructureDefinitionToElementSchemaResolver(ResourceResolver);
            TerminologyService = new TerminologyServiceAdapter(new LocalTerminologyService(ResourceResolver));

            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();
            FpCompiler = new FhirPathCompiler(symbolTable);

            Converter = new SchemaConverter(ResourceResolver);
        }

        public ValidationContext NewValidationContext() =>
            new ValidationContext(SchemaResolver, TerminologyService) { FhirPathCompiler = FpCompiler };

    }
}

