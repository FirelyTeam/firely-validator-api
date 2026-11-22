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

        [Theory]
        [InlineData("<b>Bold</b> text with <i>markup</i>", "formatted text without a surrounding div")]
        [InlineData("<br/>", "a single element that is not a div")]
        [InlineData("Just plain text", "plain text")]
        [InlineData("<div xmlns=\"http://www.w3.org/1999/xhtml\"><b>Bold</b> text with <i>markup</i></div>", "content with a surrounding div")]
        public void RenderingXhtmlExtension_StringIsTreatedAsDivContent(string value, string because)
        {
            // See https://github.com/FirelyTeam/firely-validator-api/issues/731 and
            // https://build.fhir.org/fhirpath.html#fn-htmlChecks: a string is treated as the content of a div.
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            var ext = new Extension(RenderingXhtmlValidator.RENDERING_XHTML_URL, new FhirString(value));
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.Errors.Should().BeEmpty($"{because} is valid content of a div");
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<b>unclosed")]
        public void RenderingXhtmlExtension_InvalidDivContent_FailsValidation(string value)
        {
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            var ext = new Extension(RenderingXhtmlValidator.RENDERING_XHTML_URL, new FhirString(value));
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.IsSuccessful.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void RenderingXhtmlExtension_WithNonStringValue_ReportsLiteralTypeOnce()
        {
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));

            // Used to throw from RenderingXhtmlValidator (issue #721).
            var ext = new Extension(RenderingXhtmlValidator.RENDERING_XHTML_URL, new FhirString { JsonValue = 5 });
            var host = new FhirString("text");
            host.Extension.Add(ext);

            var result = schema.Validate(host.ToPocoNode(), _fixture.NewValidationSettings());

            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("not the right type of literal for a string");
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
