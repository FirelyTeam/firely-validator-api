/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
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
    public class ExtensionSchemaValidationTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public ExtensionSchemaValidationTests(SchemaConverterFixture fixture) => _fixture = fixture;

        [Fact]
        public void UnresolvableExtensionAreJustWarnings()
        {
            ResultReport validate(Resource r)
            {
                var rs = _fixture.SchemaResolver.GetSchemaForCoreType("Resource")!;
                return rs.Validate(r.ToTypedElement(), _fixture.NewValidationContext());
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

            newP.Meta.ProfileElement.Add(new FhirUri("http://example.org/unresolvable"));
            result = validate(newP);
            result.Warnings.Count.Should().Be(0);
            result.Errors.Count.Should().Be(1);
        }

    }
}
