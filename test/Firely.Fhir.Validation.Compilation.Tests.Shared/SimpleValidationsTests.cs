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
using System.Linq;
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
            var res = schemaElement.Validate(p.ToPocoNode(), _fixture.NewValidationSettings());
            Debug.WriteLine(res.ToString());
            res.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void ValidateDateWithNoValueAndExtension()
        {
            var date = new Date() { Extension = [new("http://hl7.org/fhir/StructureDefinition/patient-birthTime", new FhirDateTime("1974-12-25T14:35:45-05:00"))] }.ToPocoNode();

            var dateSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Date");

            var results = dateSchema!.Validate(date, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue("null value is allowed for date regex");
        }

        [Fact]
        public void PatientHumanNameTooLong()
        {
            var poco = new Patient() { Name = new List<HumanName>() { new HumanName() { Family = bigString() } } };
            var patient = poco.ToPocoNode();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Patient");
            var results = schemaElement!.Validate(patient, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid");

            var ia = results.Evidence[1].Should().BeOfType<IssueAssertion>().Subject;
            ia.IssueNumber.Should().Be(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG.Code);
            ia.Location.Should().Be("Patient.name[0].family[0]");
            ia.Message.Should().Contain("is too long");
        }

        [Fact]
        public void HumanNameCorrect()
        {
            var poco = HumanName.ForFamily("Visser")
                .WithGiven("Marco")
                .WithGiven("Lourentius");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToPocoNode();

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
            var element = poco.ToPocoNode();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public void HumanNameEmptyValue()
        {
            var poco = new HumanName() { Family = "" };
            var element = poco.ToPocoNode();

            var schemaElement = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/HumanName");
            var results = schemaElement!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeFalse("HumanName is invalid, cannot be empty");
        }

        [Fact]
        public void TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToPocoNode();

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

            var element = instantPoco.ToPocoNode();
            var results = instantSchema!.Validate(element, _fixture.NewValidationSettings());

            results.Should().NotBeNull();
            results.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToPocoNode();

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
            var value = new FhirString("Non-breaking" + '\u00A0' + "space").ToPocoNode();
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
            var results = patientSchema!.Validate(patient.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, because: "patient-congregation has cardinality of 0..1");

            patient.RemoveExtension("http://hl7.org/fhir/StructureDefinition/patient-congregation");
            results = patientSchema!.Validate(patient.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(true, because: "extensions have the correct cardinality");

            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code1"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code2"));
            patient.AddExtension("http://hl7.org/fhir/StructureDefinition/patient-disability", new CodeableConcept("system", "code3"));
            results = patientSchema!.Validate(patient.ToPocoNode(), _fixture.NewValidationSettings());
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

            var results = narrativeSchema!.Validate(justWhiteSpace.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "Instance failed constraint txt-2 \"The narrative SHALL have some non-whitespace content\"");

            results = narrativeSchema!.Validate(invalidHtml.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The element 'div' in namespace 'http://www.w3.org/1999/xhtml' has invalid child element 'script' in namespace 'http://www.w3.org/1999/xhtml'. List of possible elements expected: 'p, h1, h2, h3, h4, h5, h6, div, ul, ol, dl, pre, hr, blockquote, address, table, a, br, span, bdo, map, img, tt, i, b, big, small, em, strong, dfn, code, q, samp, kbd, var, cite, abbr, acronym, sub, sup' in namespace 'http://www.w3.org/1999/xhtml'.");

        }
        
        #if STU3
        #else
        [Theory]
        [InlineData(TestProfileArtifactSource.PROFILEDEXTENSIONTYPEWITHCHILDREN)]
        [InlineData(TestProfileArtifactSource.PROFILEDEXTENSIONTYPEWITHSLICE)]
        public void ValidateProfiledRunsInvariants(string url)
        {
            var carePlan = new CarePlan()
            {
#if STU3
                Status = CarePlan.CarePlanStatus.Active,
#else
                Status = RequestStatus.Active,
#endif
                Intent = CarePlan.CarePlanIntent.Plan,
                Subject = new ResourceReference("Patient/example"),
                Extension = [ new(url, new Period(new("2017-06-02"), new("2017-06-01")))]
            };
            
            var schema = _fixture.SchemaResolver.GetSchema(Canonical.ForCoreType(carePlan.TypeName))!;
            var result = schema.Validate(carePlan.ToTypedElement(), _fixture.NewValidationSettings());
            var oo = result.ToOperationOutcome();
            oo.Success.Should().Be(false);
            oo.Issue[0].Details!.Text.Should().Contain("Instance failed constraint per-1");
        }
        #endif

        [Fact]
        public void ValidateUriStringsInExtension()
        {
            var dummy = new FhirBoolean(true);
            var extInvalid = new Extension { Url = "urn:oid:4.4.5", Value = new FhirBoolean(true)};
            var extValid = new Extension { Url = "http://example.org/ext", Value = new FhirBoolean(true)};
            
            var extSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/Extension");
            var schema = new ChildrenValidator(false, ("extension", extSchema!));
            
            dummy.Extension.Add(extInvalid);
            var results = schema.Validate(dummy.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The extension URL is invalid");
            
            dummy.Extension.Remove(extInvalid);
            dummy.Extension.Add(extValid);
            results = schema.Validate(dummy.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(true, "The extension URL is valid");
        }

        [Fact]
        public void ValidateUriStringsInDataType()
        {
            var uriInvalid = new FhirUri("urn:oid:4.4.5");
            var uriValid = new FhirUri("http://example.org/ext");
            
            var uriSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/uri");
            
            var results = uriSchema!.Validate(uriInvalid.ToPocoNode(), _fixture.NewValidationSettings());
            results.IsSuccessful.Should().Be(false, "The URI is invalid");
            
            results = uriSchema!.Validate(uriValid.ToPocoNode(), _fixture.NewValidationSettings());
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
            new FhirJsonDeserializer(new () { AnnotateLineInfo = true })
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


        /// <summary>
        /// Regression test for https://github.com/FirelyTeam/firely-validator-api/issues/631
        /// </summary>
        [Fact]
        public void SliceWithMaxOne_AllowsRepeatingChildElements()
        {
            var schema = _fixture.SchemaResolver.GetSchema(TestProfileArtifactSource.SLICEWITHREPEATINGCHILDREN)
                         ?? throw new InvalidOperationException("Profile not found");

            // ONE address (type="both") with TWO line elements — street name on line[0],
            // house number on line[1], each carrying its extension.
            // Slice cardinality max=1 is satisfied (one matching address instance).
            // address.line cardinality max=2 is satisfied (two line elements).
            // This MUST pass — the v3.1.0 bug causes it to fail.
            var streetExt = new Extension(
                "http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-streetName",
                new FhirString("Example Street"));
            var houseExt = new Extension(
                "http://hl7.org/fhir/StructureDefinition/iso21090-ADXP-houseNumber",
                new FhirString("42"));

            var streetLine = new FhirString("Example Street");
            streetLine.Extension.Add(streetExt);

            var houseLine = new FhirString("42");
            houseLine.Extension.Add(houseExt);

            var patient = new Patient
            {
                Address =
                [
                    new Address
                    {
                        Type = Address.AddressType.Both,
                        LineElement = [streetLine, houseLine],
                        City = "Testtown",
                        PostalCode = "12345"
                    }
                ]
            };

            var result = schema.Validate(patient.ToPocoNode(), _fixture.NewValidationSettings());
            result.IsSuccessful.Should().BeTrue(
                "one matching address with two line elements is valid: " +
                "slice max=1 limits the number of matching address instances, not the child line count");
        }

        /// <summary>
        /// Regression test for https://github.com/FirelyTeam/firely-validator-api/issues/631
        /// </summary>
        [Fact]
        public void SliceWithMaxOne_RejectsMultipleMatchingSliceInstances()
        {
            var schema = _fixture.SchemaResolver.GetSchema(TestProfileArtifactSource.SLICEWITHREPEATINGCHILDREN)
                         ?? throw new InvalidOperationException("Profile not found");

            // TWO addresses both with type="both" → two instances matching the StreetAddress slice → violates max=1.
            var patient = new Patient
            {
                Address =
                [
                    new Address { Type = Address.AddressType.Both, Line = ["Example Street 1"] },
                    new Address { Type = Address.AddressType.Both, Line = ["Other Avenue 2"] }
                ]
            };

            var result = schema.Validate(patient.ToPocoNode(), _fixture.NewValidationSettings());
            result.IsSuccessful.Should().BeFalse(
                "two addresses both matching the StreetAddress slice violates the slice max=1 cardinality");
        }
    }
}

