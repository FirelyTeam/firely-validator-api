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
    public class SimpleValidationsTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public SimpleValidationsTests(SchemaBuilderFixture fixture) => _fixture = fixture;

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

            var ia = results.Evidence[0].Should().BeOfType<IssueAssertion>().Subject;
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG.Code);
            ia.Location.Should().Be("Patient.name[0].family[0].value");
            ia.Message.Should().Contain("is too long");
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

        /// <summary>
        /// Regression test for https://github.com/FirelyTeam/firely-net-sdk/pull/1878
        /// </summary>
        [Fact]
        public void ValidateExtensionCardinality()
        {
            var patientSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient");

            var patient = new Patient();
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-congregation", new FhirString("place1"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-congregation", new FhirString("place2"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-cadavericDonor", new FhirBoolean(true));
            var results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationContext());
            results.IsSuccessful.Should().Be(false, because: "patient-congregation has cardinality of 0..1");

            patient.RemoveExtension("http://hl7.org/fhir/StructureDefinition/patient-congregation");
            results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationContext());
            results.IsSuccessful.Should().Be(true, because: "extensions have the correct cardinality");

            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code1"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code2"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code3"));
            results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationContext());
            results.IsSuccessful.Should().Be(true, because: "extensions have the correct cardinality");
        }

        [Fact]
        public void ValidateElementDefinitionValueType()
        {
            var elementDefSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/ElementDefinition");

            var elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirDateTime("2020-01-01"),
                MinValue = new FhirDateTime("1900-01-01"),
                MaxValue = new FhirDateTime("2100-01-01"),
                Example = new List<ElementDefinition.ExampleComponent>
                {
                    new()
                    {
                        Label = "example",
                        Value = new FhirDateTime("2020-01-01")
                    }
                }
            };

            var results = elementDefSchema!.Validate(elementDef.ToTypedElement(), _fixture.NewValidationContext());
            results.IsSuccessful.Should().Be(true);


            elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirString("2020-01-01"),
                MinValue = new FhirString("1900-01-01"),
                MaxValue = new FhirString("2100-01-01"),
                Example = new List<ElementDefinition.ExampleComponent>
                {
                    new()
                    {
                        Label = "example",
                        Value = new FhirString("2020-01-01")
                    }
                }
            };
            results = elementDefSchema!.Validate(elementDef.ToTypedElement(), _fixture.NewValidationContext());


            results.IsSuccessful.Should().Be(false, because: "Type of the fixedValue property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.IsSuccessful.Should().Be(false, because: "Type of the example.Value property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.IsSuccessful.Should().Be(false, because: "Type of the maxValue property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.IsSuccessful.Should().Be(false, because: "Type of the minValue property 'string' doesn't match with the type(s) of the element 'dateTime'");
        }
    }
}

