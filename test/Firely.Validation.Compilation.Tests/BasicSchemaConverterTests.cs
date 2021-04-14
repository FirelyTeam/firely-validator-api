/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class BasicSchemaConverterTests : IClassFixture<SchemaConverterFixture>
    {
        private readonly SchemaConverterFixture _fixture;
        private readonly ITestOutputHelper _output;

        public BasicSchemaConverterTests(SchemaConverterFixture fixture, ITestOutputHelper oh) =>
            (_output, _fixture) = (oh, fixture);

        [Fact]
        public async Task FhirDataTypeConversion()
        {
            var schema = await _fixture.SchemaResolver.GetSchemaForCoreType("BackboneElement");
            _output.WriteLine(schema!.ToJson().ToString());
        }

    }

    internal static class AvoidUriUseExtensions
    {
        public static Task<ElementSchema?> GetSchema(this IElementSchemaResolver resolver, string uri) =>
            resolver.GetSchema(new Uri(uri, UriKind.RelativeOrAbsolute));
        public static Task<ElementSchema?> GetSchemaForCoreType(this IElementSchemaResolver resolver, string typename) =>
                resolver.GetSchema("http://hl7.org/fhir/StructureDefinition/" + typename);

    }
}

