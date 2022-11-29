/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class SchemaConverterFixture
    {
        public readonly IElementSchemaResolver SchemaResolver;
        public readonly FhirPathCompiler FpCompiler;
        public readonly ICodeValidationTerminologyService ValidateCodeService;
        public readonly IAsyncResourceResolver ResourceResolver;
        public readonly SchemaConverter Converter;

        public SchemaConverterFixture()
        {
            ResourceResolver = new CachedResolver(
                new SnapshotSource(
                    new StructureDefinitionCorrectionsResolver(
                    new MultiResolver(
                    new TestProfileArtifactSource(),
                    ZipSource.CreateValidationSource()))));

            SchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(new SchemaConverterSettings(ResourceResolver));
            ValidateCodeService = new LocalTerminologyService(ResourceResolver);

            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();
            FpCompiler = new FhirPathCompiler(symbolTable);

            Converter = new SchemaConverter(new SchemaConverterSettings(ResourceResolver));
        }

        public ValidationContext NewValidationContext() =>
            new(SchemaResolver, ValidateCodeService) { FhirPathCompiler = FpCompiler };

    }
}

