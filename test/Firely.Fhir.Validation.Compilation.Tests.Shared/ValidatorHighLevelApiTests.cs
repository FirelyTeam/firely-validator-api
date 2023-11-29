/* 
 * Copyright (C) 2022, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
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

        [Fact]
        public void ValidatesAndReportsError()
        {
            var p = new Patient() { Deceased = new FhirString("wrong") };
            var validator = new Validator(_fixture.ValidateCodeService, _fixture.ResourceResolver, null);
            var result = validator.Validate(p, Canonical.ForCoreType("Patient").ToString());

            result.Errors.Should().ContainSingle().Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE.Code);
            result.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void NoReferenceResolution()
        {
            // First, without a resolver, we do not follow references and thus do not validate the referenced resource.
            var p = new Patient() { ManagingOrganization = new ResourceReference("http://example.com/orgA") };
            var validator = new Validator(_fixture.ValidateCodeService, _fixture.ResourceResolver, null);
            validator.Validate(p, Canonical.ForCoreType("Patient").ToString()).IsSuccessful.Should().BeTrue();

            // Now with reference resolution
            var or = new InMemoryExternalReferenceResolver() { ["http://example.com/orgA"] = new Organization() };
            validator = new Validator(_fixture.ValidateCodeService, _fixture.ResourceResolver, or);
            var result = validator.Validate(p, Canonical.ForCoreType("Patient").ToString());

            // Organization should fail constraint org-1
            result.Errors.Should().ContainSingle().Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT.Code);
        }

        [Fact]
        public void SkipConstraintValidation()
        {
            var o = new Organization();
            var validator = new Validator(_fixture.ValidateCodeService, _fixture.ResourceResolver, null);

            // validate with constraint validation, it should fail on org-1
            var result = validator.Validate(o, Canonical.ForCoreType("Organization").ToString());
            result.Errors.Should().ContainSingle().Which.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT.Code);

            // now, skip constraint validation
            validator.SkipConstraintValidation = true;
            result = validator.Validate(o, Canonical.ForCoreType("Organization").ToString());
            result.IsSuccessful.Should().BeTrue();
        }
    }
}
#endif