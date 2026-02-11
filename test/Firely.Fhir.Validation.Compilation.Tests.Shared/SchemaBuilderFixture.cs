/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class SchemaBuilderFixture
    {
        internal readonly IElementSchemaResolver SchemaResolver;
        public readonly FhirPathCompiler FpCompiler;
        public readonly ICodeValidationTerminologyService ValidateCodeService;
        public readonly IAsyncResourceResolver ResourceResolver;
        internal readonly SchemaBuilder Builder;

        public SchemaBuilderFixture()
        {
            ResourceResolver = new CachedResolver(
                new SnapshotSource(
                    new StructureDefinitionCorrectionsResolver(
                        new MultiResolver(
                            new TestProfileArtifactSource(),
                            ZipSource.CreateValidationSource()))));

            SchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(ResourceResolver);
            ValidateCodeService = new LocalTerminologyService(ResourceResolver);

            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();
            FpCompiler = new FhirPathCompiler(symbolTable);

            Builder = new SchemaBuilder(ResourceResolver, new[] { new StandardBuilders(ResourceResolver) });
        }

        internal ValidationSettings NewValidationSettings() =>
            new(SchemaResolver, ValidateCodeService) { FhirPathCompiler = FpCompiler };

    }
}

