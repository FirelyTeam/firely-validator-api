using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests;

public class ExtensionContextComponentTests  : IClassFixture<SchemaBuilderFixture>
{
    internal SchemaBuilderFixture _fixture;
    
    public ExtensionContextComponentTests(SchemaBuilderFixture fixture) => _fixture = fixture;

    [Fact]
    public void CreatesExtensionContextSchema()
    {
        var schema = _fixture.SchemaResolver.GetSchema(TestProfileArtifactSource.CONTEXTCONSTRAINEDEXTENSION);
        schema.Should().NotBeNull();
        var json = schema.ToJson();

        json["context"].Should().NotBeNull();
    }
}