/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// Until we have Marco's IScopedNodeOnPoco adapter, I cannot write R5 tests using just the "shared" R4+ validator.
#if !R5

namespace Firely.Fhir.Validation.Tests
{
    [Trait("Category", "Validation")]
    public class ValidatorHighLevelApiTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public ValidatorHighLevelApiTests(SchemaBuilderFixture fixture)
        {
            _fixture = fixture;
        }

        private IEnumerable<string> getErrorCodes(OperationOutcome oo) => oo.Issue.SelectMany(i => i.Details.Coding).Where(cd => cd.System == "http://hl7.org/fhir/dotnet-api-operation-outcome").Select(c => c.Code.ToString());


        [Fact]
        public void ValidatesAndReportsError()
        {
            var p = new Patient() { Deceased = new FhirString("wrong") };
            var validator = new Validator(_fixture.ResourceResolver, _fixture.ValidateCodeService, null);
            var result = validator.Validate(p, Canonical.ForCoreType("Patient").ToString());

            getErrorCodes(result).Should().ContainSingle(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE.Code.ToString());
            result.Success.Should().BeFalse();

            result = validator.Validate(p);
            getErrorCodes(result).Should().ContainSingle(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE.Code.ToString());
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void NoReferenceResolution()
        {
            // First, without a resolver, we do not follow references and thus do not validate the referenced resource.
            var p = new Patient() { ManagingOrganization = new ResourceReference("http://example.com/orgA") };
            var validator = new Validator(_fixture.ResourceResolver, _fixture.ValidateCodeService, null);
            validator.Validate(p, Canonical.ForCoreType("Patient").ToString()).Success.Should().BeTrue();

            // Now with reference resolution
            var or = new InMemoryExternalReferenceResolver() { ["http://example.com/orgA"] = new Organization() };
            validator = new Validator(_fixture.ResourceResolver, _fixture.ValidateCodeService, or);
            var result = validator.Validate(p, Canonical.ForCoreType("Patient").ToString());

            // Organization should fail constraint org-1
            getErrorCodes(result).Should().ContainSingle(Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT.Code.ToString());
        }

        [Fact]
        public void SkipConstraintValidation()
        {
            var o = new Organization();
            var settings = new ValidationSettings();
            var validator = new Validator(_fixture.ResourceResolver, _fixture.ValidateCodeService, settings: settings);

            // validate with constraint validation, it should fail on org-1
            var result = validator.Validate(o, Canonical.ForCoreType("Organization").ToString());
            getErrorCodes(result).Should().ContainSingle(Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT.Code.ToString());

            // now, skip constraint validation
            settings.SetSkipConstraintValidation(true);
            result = validator.Validate(o, Canonical.ForCoreType("Organization").ToString());
            result.Success.Should().BeTrue();
        }
    }
}
#endif