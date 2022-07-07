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
        public void PatientHumanNameTooLong()
        {
            var poco = new Patient() { Name = new List<HumanName>() { new HumanName() { Family = bigString() } } };
            var patient = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient");
            var results = schemaElement!.Validate(patient, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is valid");

            results.Evidence[0].Should().BeOfType<IssueAssertion>();
            var referenceObject = new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "Patient.name[0].family[0].value", "too long!");
            results.Evidence[0]
                .Should()
                .BeEquivalentTo(referenceObject, options => options.Excluding(o => o.Message));
        }

        [Fact]
        public void HumanNameCorrect()
        {
            var poco = HumanName.ForFamily("Visser")
                .WithGiven("Marco")
                .WithGiven("Lourentius");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var results = schemaElement!.Validate(element, _fixture.NewValidationContext());
            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue("HumanName is valid");
        }

        [Fact]
        public void HumanNameTooLong()
        {
            var poco = HumanName.ForFamily(bigString())
                .WithGiven(bigString())
                .WithGiven("Maria");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var results = schemaElement!.Validate(element, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public void TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var x = schemaElement.ToJson().ToString();

            var results = schemaElement!.Validate(element, _fixture.NewValidationContext());
            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid, cannot be empty");
        }

        [Fact]
        public void TestInstance()
        {
            var instantSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/instant");

            var instantPoco = new Instant(DateTimeOffset.Now);

            var element = instantPoco.ToTypedElement();
            var results = instantSchema!.Validate(element, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToTypedElement();

            var stringSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/string");

            var results = stringSchema!.Validate(fhirString, _fixture.NewValidationContext());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("fhirString is not valid");
        }

        /// <summary>
        /// Regression test for https://github.com/FirelyTeam/firely-net-sdk/issues/1563
        /// </summary>
        [Fact]
        public void ValidateNonBreakingWhitespaceInString()
        {
            var value = new FhirString("Non-breaking" + '\u00A0' + "space").ToTypedElement();
            var stringSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/string");
            var result = stringSchema!.Validate(value, _fixture.NewValidationContext());
            Assert.True(result.IsSuccessful);
        }
    }
}

