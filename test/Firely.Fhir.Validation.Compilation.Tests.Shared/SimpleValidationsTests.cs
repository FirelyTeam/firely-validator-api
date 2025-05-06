
/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public void ParametersPart_ValidatesFhirPathConstraints()
        {
            var p = new Parameters() 
            {
                Parameter = [ new () 
                {
                    Name = "Test",
                    Part = [ new()
                    {
                        Name = "Invalid"
                    } ]
                } ]
            };
            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Parameters") ?? throw new InvalidOperationException();
            var json = schemaElement.ToJson();
            var res = schemaElement.Validate(p.ToTypedElement(), _fixture.NewValidationSettings());
            Debug.WriteLine(res.ToString());
            res.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void PatientHumanNameTooLong()
        {
            var poco = new Patient() { Name = new List<HumanName>() { new HumanName() { Family = bigString() } } };
            var patient = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient");
            var results = schemaElement!.Validate(patient, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid");

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
            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());
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
            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public void HumanNameEmptyValue()
        {
            var poco = new HumanName() { Family = "" };
            var element = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid, cannot be empty");
        }

        [Fact]
        public void TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToTypedElement();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");

            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());
            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid, cannot be empty");
        }

        [Fact]
        public void TestInstance()
        {
            var instantSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/instant");

            var instantPoco = new Instant(DateTimeOffset.Now);

            var element = instantPoco.ToTypedElement();
            var results = instantSchema!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToTypedElement();

            var stringSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/string");

            var results = stringSchema!.Validate(fhirString, _fixture.NewValidationSettings());

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
            var result = stringSchema!.Validate(value, _fixture.NewValidationSettings());
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
            var results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, because: "patient-congregation has cardinality of 0..1");

            patient.RemoveExtension("http://hl7.org/fhir/StructureDefinition/patient-congregation");
            results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(true, because: "extensions have the correct cardinality");

            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code1"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code2"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code3"));
            results = patientSchema!.Validate(patient.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(true, because: "extensions have the correct cardinality");
        }

        [Fact]
        public void ValidateNarrativeInvariants()
        {
            var justWhiteSpace = new Narrative
            {
                Status = Narrative.NarrativeStatus.Additional,
                Div = " "
            };

            var invalidHtml = new Narrative
            {
                Status = Narrative.NarrativeStatus.Additional,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><script> document.getElementById(\"demo\").innerHTML = \"Hello JavaScript!\"; </script></div>"
            };

            var narrativeSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Narrative");

            var results = narrativeSchema!.Validate(justWhiteSpace.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "Instance failed constraint txt-2 \"The narrative SHALL have some non-whitespace content\"");

            results = narrativeSchema!.Validate(invalidHtml.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The element 'div' in namespace 'http://www.w3.org/1999/xhtml' has invalid child element 'script' in namespace 'http://www.w3.org/1999/xhtml'. List of possible elements expected: 'p, h1, h2, h3, h4, h5, h6, div, ul, ol, dl, pre, hr, blockquote, address, table, a, br, span, bdo, map, img, tt, i, b, big, small, em, strong, dfn, code, q, samp, kbd, var, cite, abbr, acronym, sub, sup' in namespace 'http://www.w3.org/1999/xhtml'.");

        }

        [Fact]
        public void ValidateUriStringsInExtension()
        {
            var extInvalid = new Extension { Url = "urn:oid:4.4.5", Value = new FhirBoolean(true)};
            var extValid = new Extension { Url = "http://example.org/ext", Value = new FhirBoolean(true)};
            
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            
            var results = extSchema!.Validate(extInvalid.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The extension URL is invalid");
            
            results = extSchema!.Validate(extValid.ToTypedElement(), _fixture.NewValidationSettings());
            var err = results.Errors;
            results.IsSuccessful.Should().Be(true, "The extension URL is valid");
        }

        [Fact]
        public void ValidateUriStringsInDataType()
        {
            var uriInvalid = new FhirUri("urn:oid:4.4.5");
            var uriValid = new FhirUri("http://example.org/ext");
            
            var uriSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/uri");
            
            var results = uriSchema!.Validate(uriInvalid.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The URI is invalid");
            
            results = uriSchema!.Validate(uriValid.ToTypedElement(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(true, "The URI is valid");
        }
        
        [Fact]
        public void OperationOutcome_IncludesProfileAuthorityExtension()
        {
            var p = new Patient()
            {
                Meta = new() { Profile = new[] { TestProfileArtifactSource.PATIENTWITHPROFILEDREFS } },
                Deceased = new FhirString("wrong")
            };
            var schema = _fixture.SchemaResolver.GetSchema(Canonical.ForCoreType("Resource"))!;
            var result = schema.Validate(p.ToTypedElement(), _fixture.NewValidationSettings());
            var oo = result.ToOperationOutcome();
            oo.Success.Should().BeFalse();
            oo.Issue[0].GetExtensionValue<FhirUri>("http://hl7.org/fhir/StructureDefinition/operationoutcome-authority")
                .Should().BeEquivalentTo(new FhirUri(TestProfileArtifactSource.PATIENTWITHPROFILEDREFS));
        }

        [Fact]
        public void OperationOutcome_FromTypedElement_IncludesLineNumberExtensions()
        {
            var json = """
                       {
                         "resourceType": "Patient",
                         "gender": "alien"
                       }
                       """;
            var typedElement = FhirJsonNode.Parse(json).ToTypedElement(ModelInfo.ModelInspector);
            validateLineNumberExtension(typedElement, 3, 19, "BindingValidator");
        }

        [Fact]
        public void OperationOutcome_FromPoco_IncludesLineNumberExtensions()
        {
            var json = """
                       {
                         "resourceType": "Patient",
                         "gender": "alien"
                       }
                       """;
            new FhirJsonPocoDeserializer(new FhirJsonPocoDeserializerSettings() { AnnotateLineInfo = true })
                .TryDeserializeResource(json, out var resource, out _);
            var typedElement = resource!.ToTypedElement();
            validateLineNumberExtension(typedElement, 3, 20, "BindingValidator");
        }

        private void validateLineNumberExtension(ITypedElement typedElement, int line, int column, string? source)
        {
            var schema = _fixture.SchemaResolver.GetSchema(Canonical.ForCoreType("Resource"))!;
            var result = schema.Validate(typedElement, _fixture.NewValidationSettings());
            var oo = result.ToOperationOutcome();
            oo.Success.Should().BeFalse();
            oo.Issue[0].GetExtensionValue<Integer>("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-line")
                .Should().BeEquivalentTo(new Integer(line));
            oo.Issue[0].GetExtensionValue<Integer>("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-col")
                .Should().BeEquivalentTo(new Integer(column));
            oo.Issue[0].GetExtensionValue<FhirString>("http://hl7.org/fhir/StructureDefinition/operationoutcome-issue-source")
                .Should().BeEquivalentTo(new FhirString(source));
        }
    }
}

