/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    public class SimpleValidationsTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public SimpleValidationsTests(SchemaConverterFixture fixture) => _fixture = fixture;

        private static string bigString()
        {
            var sb = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 1024; i++)
            {
                sb.Append('x');
            }

            var sub = sb.ToString();

            sb = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 1024; i++)
            {
                sb.Append(sub);
            }
            sb.Append("more");
            return sb.ToString();
        }

        [Fact]
        public async T.Task PatientHumanNameTooLong()
        {
            var poco = new Patient() { Name = new List<HumanName>() { new HumanName() { Family = bigString() } } };
            var patient = poco.ToTypedElement();

            var schemaElement = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/Patient", UriKind.Absolute));
            var results = await schemaElement!.Validate(patient, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is valid");
            // first is trace
            results.Evidence[1].Should().BeOfType<IssueAssertion>();
            var referenceObject = new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "Patient.name[0].family[0].value", "too long!");
            results.Evidence[1]
                .Should()
                .BeEquivalentTo(referenceObject, options => options.Excluding(o => o.Message));
        }

        [Fact]
        public async T.Task HumanNameCorrect()
        {
            var poco = HumanName.ForFamily("Visser")
                .WithGiven("Marco")
                .WithGiven("Lourentius");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));
            var results = await schemaElement!.Validate(element, _fixture.NewValidationContext());
            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue("HumanName is valid");
        }

        [Fact]
        public async T.Task HumanNameTooLong()
        {
            var poco = HumanName.ForFamily(bigString())
                .WithGiven(bigString())
                .WithGiven("Maria");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));
            var results = await schemaElement!.Validate(element, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public async T.Task TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));

            var results = await schemaElement!.Validate(element, _fixture.NewValidationContext());
            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is valid, cannot be empty");
        }

        [Fact]
        public async T.Task TestInstance()
        {
            var instantSchema = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/instant", UriKind.Absolute));

            var instantPoco = new Instant(DateTimeOffset.Now);

            var element = instantPoco.ToTypedElement();
            var results = await instantSchema!.Validate(element, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public async T.Task ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToTypedElement();

            var stringSchema = await _fixture.SchemaResolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/string", UriKind.Absolute));

            var results = await stringSchema!.Validate(fhirString, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("fhirString is not valid");
        }
    }
}

