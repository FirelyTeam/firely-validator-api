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
using Hl7.Fhir.Support;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    [Trait("Category", "Validation")]
    public class ResourceSchemaValidationTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public ResourceSchemaValidationTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        private ITypedElement? resolveTestData(string uri, string location)
        {
            string Url = "http://test.org/fhir/Organization/3141";
            Organization dummy = new() { Id = "3141", Name = "Dummy" };
            return uri == Url ? dummy.ToTypedElement() : null;
        }


        [Fact]
        public void DoesValidateBasedOnActualType()
        {
            var p = new Patient() { Deceased = new FhirString("wrong") };
            var schema = _fixture.SchemaResolver.GetSchema(Canonical.ForCoreType("Resource"))!;
            var result = schema.Validate(p.ToTypedElement(), _fixture.NewValidationSettings());
            result.IsSuccessful.Should().BeFalse();
            result.Evidence.Should().ContainSingle(ass => ass is IssueAssertion && ((IssueAssertion)ass).IssueNumber == Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE.Code);
        }

        [Fact]
        public void AvoidsRedoingProfileValidation()
        {
            var all = new Bundle() { Type = Bundle.BundleType.Collection };
            var org1 = new Organization() { Id = "org1", Name = "Organization 1" };
            var org2 = new Organization() { Id = "org2", Name = "Organization 2", Meta = new() { Profile = new[] { TestProfileArtifactSource.PROFILEDORG2 } } };

            all.Entry.Add(new() { FullUrl = refr("org1"), Resource = org1 });
            all.Entry.Add(new() { FullUrl = refr("org2"), Resource = org2 });

            var bothRef = new List<ResourceReference>()
            {
                new ResourceReference("#refme"),
                new ResourceReference(refr("org1")),
                new ResourceReference(refr("org2")),
                new ResourceReference("http://test.org/fhir/Organization/3141")
            };

            var pat1 = new Patient()
            {
                Meta = new() { Profile = new[] { TestProfileArtifactSource.PATIENTWITHPROFILEDREFS } },
                Id = "pat1",
                GeneralPractitioner = bothRef,
                ManagingOrganization = new(refr("org1")),
                BirthDate = new("2011crap")
            };

            pat1.Contained.Add(new Organization { Id = "refme", Name = "referred" });

            var pat2 = new Patient()
            {
                Meta = new() { Profile = new[] { TestProfileArtifactSource.PATIENTWITHPROFILEDREFS } },
                Id = "pat2",
                GeneralPractitioner = bothRef,
                ManagingOrganization = new(refr("org2"))
            };

            pat2.Contained.Add(new Organization { Id = "refme", Name = "referred" });

            all.Entry.Add(new() { FullUrl = refr("pat1"), Resource = pat1 });
            all.Entry.Add(new() { FullUrl = refr("pat2"), Resource = pat2 });

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Bundle");
            var vc = _fixture.NewValidationSettings();
            vc.ResolveExternalReference = resolveTestData;

            var validationState = new ValidationState();
            var result = schemaElement!.ValidateInternal(new ScopedNode(all.ToTypedElement()).AsScopedNode(), vc, validationState);
            result.Result.Should().Be(ValidationResult.Failure);
            var issues = result.Evidence.OfType<IssueAssertion>().ToList();
            issues.Count.Should().Be(1);  // Bundle.entry[2].resource[0] is validated twice against different profiles.
            issues.ForEach(i => i.Message.Should().Contain("does not match regex"));

            validationState.Global.ResourcesValidated.Should().Be(9);
            validationState.Global.RunValidations.Count.Should().Be(9);

            static string refr(string x) => "http://test.org/fhir/" + x;
        }
    }
}
