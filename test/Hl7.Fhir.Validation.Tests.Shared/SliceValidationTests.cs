using Hl7.Fhir.Model;
using Xunit;

namespace Hl7.Fhir.Validation.Tests
{
    [Trait("Category", "Validation")]
    public class SliceValidationTests : IClassFixture<ValidationFixture>
    {
        private readonly Validator _validator;

        public SliceValidationTests(ValidationFixture fixture, Xunit.Abstractions.ITestOutputHelper _)
        {
            _validator = fixture.GetNew();
        }

        [Fact]
        public void TestDiscriminatedTelecomSliceUse()
        {
            var p = new Patient();

            // Incorrect "home" use for slice "phone"
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Use = ContactPoint.ContactPointUse.Home, Value = "e.kramer@furore.com" });

            // Incorrect use of "use" for slice "other"
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Other, Use = ContactPoint.ContactPointUse.Home, Value = "http://nu.nl" });

            // Correct use of slice "other"
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Other, Value = "http://nu.nl" });

            // Correct "work" use for slice "phone", but out of order
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Use = ContactPoint.ContactPointUse.Work, Value = "ewout@di.nl" });
            DebugDumpOutputXml(p);

            var outcome = _validator.Validate(p, "http://example.com/StructureDefinition/patient-telecom-slice-ek");
            DebugDumpOutputXml(outcome);
            Assert.False(outcome.Success);
            Assert.Equal(3, outcome.Errors);
            Assert.Equal(0, outcome.Warnings);  // 11 terminology warnings, reset when terminology is working again
            var repr = outcome.ToString();
            Assert.Contains("Element matches slice 'phone', but this is out of order", repr);
            Assert.Contains("Value 'home' is not exactly equal to fixed value 'work'", repr);
            Assert.Contains("Instance count is 1, which is not within", repr);
        }


        [Fact]
        public void TestTelecomReslicing()
        {
            var p = new Patient();

            // Incorrect "old" use for closed slice telecom:email
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Email, Use = ContactPoint.ContactPointUse.Home, Value = "e.kramer@furore.com" });
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Email, Use = ContactPoint.ContactPointUse.Old, Value = "ewout@di.nl" });

            // Too many for telecom:other/home
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Other, Use = ContactPoint.ContactPointUse.Home, Value = "http://nu.nl" });
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Other, Use = ContactPoint.ContactPointUse.Home, Value = "http://nos.nl" });

            // Out of order openAtEnd
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Fax, Use = ContactPoint.ContactPointUse.Work, Value = "+31-20-6707070" });

            // For the open slice in telecom:other
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Other, Use = ContactPoint.ContactPointUse.Temp, Value = "skype://crap" });  // open slice

            // Out of order (already have telecom:other)
            p.Telecom.Add(new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Use = ContactPoint.ContactPointUse.Home, Value = "+31-6-39015765" });

            DebugDumpOutputXml(p);

            var outcome = _validator.Validate(p, "http://example.com/StructureDefinition/patient-telecom-reslice-ek");
            DebugDumpOutputXml(outcome);
            Assert.False(outcome.Success);
            Assert.Equal(7, outcome.Errors);
            Assert.Equal(0, outcome.Warnings);
            var repr = outcome.ToString();
            Assert.Contains("not within the specified cardinality of 1..5 (at Patient)", repr);
            Assert.Contains("which is not allowed for an open-at-end group (at Patient.telecom[5])", repr);
            Assert.Contains("a previous element already matched slice 'Patient.telecom:other' (at Patient.telecom[6])", repr);
            Assert.Contains("group at 'Patient.telecom:email' is closed. (at Patient.telecom[1])", repr);
        }



        [Fact]
        public void TestDirectTypeSliceValidation()
        {
            test("b:t", new FhirBoolean(true), true);
            test("b:f", new FhirBoolean(false), false, "not exactly equal to fixed value");  // fixed to true
            test("s:hi", new FhirString("hi!"), true);
            test("s:there", new FhirString("there"), false, "not exactly equal to fixed value");  // fixed to hi!
            test("c:10kg", new Quantity(10m, "kg"), true);
            test("c:cc", new CodeableConcept("http://nos.nl", "bla"), true);
            test("s:there", new FhirString("there"), false, "not exactly equal to fixed value");  // fixed to hi!
            test("fdt:f", FhirDateTime.Now(), false, "not one of the allowed choices");

            void test(string title, DataType v, bool success, string? fragment = null)
            {
                var t = new Observation()
                {
                    Status = ObservationStatus.Final,
                    Code = new CodeableConcept("http://bla.ml", "blie"),
                    Value = v
                };

                var outcome = _validator.Validate(t, "http://example.org/fhir/StructureDefinition/obs-with-sliced-value");

                //if (!outcome.Success)
                //    output.WriteLine(outcome.ToString());

                if (success)
                    Assert.True(outcome.Success, title);
                else
                    Assert.False(outcome.Success, title);

                if (fragment != null)
                    Assert.Contains(fragment, outcome.ToString());
            }
        }

        private void DebugDumpOutputXml(Base fragment)
        {
#if DUMP_OUTPUT
            // Commented out to not fill up the CI builds output log
            var doc = System.Xml.Linq.XDocument.Parse(new Serialization.FhirXmlSerializer().SerializeToString(fragment));
            output.WriteLine(doc.ToString(System.Xml.Linq.SaveOptions.None));
#endif
        }

        [Fact]
        public void SupportsExistsSlicing()
        {
            var p = new Patient();

            var iWithoutUse = new Identifier { System = "http://bla.nl" };
            p.Identifier.Add(iWithoutUse);

            var outcome = _validator.Validate(p, "http://validationtest.org/fhir/StructureDefinition/PatientExistsSlicing");
            Assert.False(outcome.Success);  // system should be 0..0 because we don't have a use

            iWithoutUse.Use = Identifier.IdentifierUse.Official;
            outcome = _validator.Validate(p, "http://validationtest.org/fhir/StructureDefinition/PatientExistsSlicing");
            Assert.True(outcome.Success);

            p.Identifier[0].System = null;
            outcome = _validator.Validate(p, "http://validationtest.org/fhir/StructureDefinition/PatientExistsSlicing");
            Assert.False(outcome.Success);  // system should be 1..1 now
        }
    }
}
