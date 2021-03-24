﻿using Firely.Fhir.Validation;
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

        public SchemaConverterFixture()
        {
            ResourceResolver = new CachedResolver(
                new SnapshotSource(new MultiResolver(
                    new TestProfileArtifactSource(),
                    ZipSource.CreateValidationSource())));

            SchemaResolver = new ElementSchemaResolver(ResourceResolver);
            TerminologyService = new TerminologyServiceAdapter(new LocalTerminologyService(ResourceResolver));

            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();
            FpCompiler = new FhirPathCompiler(symbolTable);
        }
    }
}

