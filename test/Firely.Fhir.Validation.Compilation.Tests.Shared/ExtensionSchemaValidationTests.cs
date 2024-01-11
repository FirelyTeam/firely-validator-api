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
                return rs.Validate(r.ToTypedElement(), _fixture.NewValidationSettings());
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
