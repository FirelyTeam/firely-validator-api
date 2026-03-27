/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{

    [TestClass]
    public class ExtensionSchemaValidationTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public ExtensionSchemaValidationTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        [Fact]
        public void UnresolvableExtensionAreJustWarnings()
        {
            ResultReport validate(Resource r)
            {
                var rs = _fixture.SchemaResolver.GetSchemaForCoreType("Resource")!;
                return rs.Validate(r.ToPocoNode(), _fixture.NewValidationSettings());
            }

            var p = new Patient
            {
                Active = true
            };

            p.AddExtension("http://nu.nl", new FhirBoolean(false), isModifier: false);

            var result = validate(p);
            result.Result.Should().Be(ValidationResult.Success);
            result.Warnings.Count.Should().Be(1);
            result.Errors.Count.Should().Be(0);

            p.AddExtension("http://nu.nl/modifier", new FhirBoolean(false), isModifier: true);
            result = validate(p);
            result.IsSuccessful.Should().BeFalse();
            result.Warnings.Count.Should().Be(1);
            result.Errors.Count.Should().Be(1);

            var newP = new Patient
            {
                Active = true,
                Meta = new()
            };

            #if STU3
            newP.Meta.ProfileElement.Add(new FhirUri("http://example.org/unresolvable"));
            #else 
            newP.Meta.ProfileElement.Add(new Hl7.Fhir.Model.Canonical("http://example.org/unresolvable"));
            #endif
            result = validate(newP);
            result.Warnings.Count.Should().Be(0);
            result.Errors.Count.Should().Be(1);
        }

        [Fact]
        public void RenderingXhtmlExtension_WithValidXhtml_PassesValidation()
        {
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            var ext = new Extension(RenderingXhtmlValidator.RENDERING_XHTML_URL,
                new FhirString("<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Hello World</p></div>"));
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.Errors.Should().BeEmpty("valid XHTML should not produce XHTML validation errors");
        }

        [Fact]
        public void RenderingXhtmlExtension_WithInvalidXhtml_FailsValidation()
        {
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            // <script> is not allowed in FHIR narrative XHTML
            var ext = new Extension(RenderingXhtmlValidator.RENDERING_XHTML_URL,
                new FhirString("<div xmlns=\"http://www.w3.org/1999/xhtml\"><script>alert('xss')</script></div>"));
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.IsSuccessful.Should().BeFalse("invalid XHTML in rendering-xhtml extension should fail validation");
            result.Errors.Should().NotBeEmpty("there should be at least one XHTML validation error");
        }

        [Fact]
        public void OtherStringExtension_WithXhtmlLikeContent_IsNotValidatedAsXhtml()
        {
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            // Same content that would fail for rendering-xhtml, but this is a different extension URL
            var ext = new Extension("http://example.org/my-string-extension",
                new FhirString("<div xmlns=\"http://www.w3.org/1999/xhtml\"><script>alert('xss')</script></div>"));
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.Errors.Should().BeEmpty("non-rendering-xhtml string extensions must not be validated as XHTML");
        }

    }
}
