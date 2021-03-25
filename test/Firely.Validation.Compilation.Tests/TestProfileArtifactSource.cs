﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Validation.Compilation.Tests
{
    internal class TestProfileArtifactSource : IResourceResolver
    {
        public const string PATTERNSLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/PatternSliceTestcase";
        public const string VALUESLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcase";
        public const string VALUESLICETESTCASEOPEN = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcaseOpen";
        public const string VALUESLICETESTCASEWITHDEFAULT = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcaseWithDefault";
        public const string DISCRIMINATORLESS = "http://validationtest.org/fhir/StructureDefinition/DiscriminatorlessTestcase";
        public const string TYPEANDPROFILESLICE = "http://validationtest.org/fhir/StructureDefinition/TypeAndProfileTestcase";
        public const string REFERENCEDTYPEANDPROFILESLICE = "http://validationtest.org/fhir/StructureDefinition/ReferencedTypeAndProfileTestcase";
        public const string EXISTSLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ExistSliceTestcase";
        public const string RESLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase";

        public List<StructureDefinition> TestProfiles = new List<StructureDefinition>
        {
            // The next two test cases should produce the same outcome, since value and pattern
            // discriminators have been merged (at least, in R5).
            buildValueOrPatternSliceTestcase(PATTERNSLICETESTCASE),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASE),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASEWITHDEFAULT),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASEOPEN),
            buildValueOrPatternSliceTestcase(DISCRIMINATORLESS),
            buildTypeAndProfileSlice(),
            buildReferencedTypeAndProfileSlice(),
            buildExistSliceTestcase(),
            buildResliceTestcase()
        };

        //private static StructureDefinition slicingWithCodeableConcept()
        //{
        //    var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/ObservationSlicingCodeableConcept", "ObservationSlicingCodeableConcept",
        //               "Test Observation with slicing on value[x], first slice CodeableConcept", FHIRAllTypes.Observation);

        //    var cons = result.Differential.Element;

        //    var slicingIntro = new ElementDefinition("Observation.value[x]")
        //       .WithSlicingIntro(ElementDefinition.SlicingRules.Closed)
        //       .OfType(FHIRAllTypes.CodeableConcept);

        //    cons.Add(slicingIntro);

        //    cons.Add(new ElementDefinition("Observation.value[x]")
        //    {
        //        ElementId = "Observation.value[x]:valueCodeableConcept",
        //        SliceName = "valueCodeableConcept",
        //    }.OfType(FHIRAllTypes.CodeableConcept)
        //     .WithBinding("http://somewhere/something", BindingStrength.Required));

        //    return result;
        //}

        //private static StructureDefinition slicingWithQuantity()
        //{
        //    var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/ObservationValueSlicingQuantity", "ObservationSlicingQuantity",
        //               "Test Observation with slicing on value[x], first slice Quantity", FHIRAllTypes.Observation,
        //               "http://validationtest.org/fhir/StructureDefinition/ObservationSlicingCodeableConcept");

        //    var cons = result.Differential.Element;

        //    var slicingIntro = new ElementDefinition("Observation.value[x]")
        //       .WithSlicingIntro(ElementDefinition.SlicingRules.Closed)
        //       .OfType(FHIRAllTypes.Quantity);

        //    cons.Add(slicingIntro);

        //    cons.Add(new ElementDefinition("Observation.value[x]")
        //    {
        //        ElementId = "Observation.value[x]:valueQuantity",
        //        SliceName = "valueQuantity",
        //    }.OfType(FHIRAllTypes.Quantity)
        //     .WithBinding("http://somewhere/something-else", BindingStrength.Required));

        //    return result;
        //}

        private static StructureDefinition buildValueOrPatternSliceTestcase(string canonical)
        {
            var usePattern = canonical == PATTERNSLICETESTCASE;
            var withDefault = canonical == VALUESLICETESTCASEWITHDEFAULT;
            var discriminatorless = canonical == DISCRIMINATORLESS;
            var open = canonical == VALUESLICETESTCASEOPEN;

            var result = createTestSD(canonical, "ValueOrPatternSlicingTestcase",
                       "Testcase with a pattern/value slice on Patient.identifier", FHIRAllTypes.Patient);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var slicingIntro = new ElementDefinition("Patient.identifier");

            if (!discriminatorless)
                slicingIntro.WithSlicingIntro(!open ? ElementDefinition.SlicingRules.Closed : ElementDefinition.SlicingRules.Open,
                (usePattern ? ElementDefinition.DiscriminatorType.Pattern : ElementDefinition.DiscriminatorType.Value, "system"));
            else
                slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed);

            cons.Add(slicingIntro);

            // First slice, should slice on the "fixed" of system
            cons.Add(new ElementDefinition("Patient.identifier")
            {
                ElementId = "Patient.identifier:fixed",
                SliceName = "Fixed"
            });

            cons.Add(new ElementDefinition("Patient.identifier.system")
            {
                ElementId = "Patient.identifier:fixed.system",
            }.Value(fix: new FhirUri("http://example.com/some-bsn-uri")));

            // Second slice, should slice on the pattern + binding of system
            // When we're testing @default slice, we'll turn this into a default slice
            cons.Add(new ElementDefinition("Patient.identifier")
            {
                ElementId = "Patient.identifier:PatternBinding",
                SliceName = withDefault ? "@default" : "PatternBinding"
            });

            cons.Add(new ElementDefinition("Patient.identifier.system")
            {
                ElementId = "Patient.identifier:PatternBinding.system",
            }
            .Value(pattern: new FhirUri("http://example.com/someuri"))
            .WithBinding("http://example.com/demobinding", BindingStrength.Required)
            );

            return result;
        }

        private static StructureDefinition buildTypeAndProfileSlice()
        {
            var result = createTestSD(TYPEANDPROFILESLICE, "TypeAndProfileSliceTestcase",
                       "Testcase with a type and profile slice on Questionnaire.item.enableWhen.answer[x]", FHIRAllTypes.Questionnaire);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var slicingIntro = new ElementDefinition("Questionnaire.item.enableWhen");

            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Profile, "question"),
                (ElementDefinition.DiscriminatorType.Type, "answer"));
            cons.Add(slicingIntro);

            // First slice is on question[http://example.com/profile1] and answer[String]
            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen")
            {
                ElementId = "Questionnaire.item.enableWhen:string",
                SliceName = "string"
            });

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.question")
            {
                ElementId = "Questionnaire.item.enableWhen:string.question",
            }.OfType(FHIRAllTypes.String, new[] { "http://example.com/profile1" }));

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.answer[x]")
            {
                ElementId = "Questionnaire.item.enableWhen:string.answer[x]",
            }.OfType(FHIRAllTypes.String));

            // Second slice is on answer[Boolean], but no profile set on question
            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen")
            {
                ElementId = "Questionnaire.item.enableWhen:boolean",
                SliceName = "boolean"
            });

            //It's unclear whether having once of the two discriminating values
            //missing is an error. When it is, undocument the code below.
            //cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.question")
            //{
            //    ElementId = "Questionnaire.item.enableWhen:boolean.question",
            //}.OfType(FHIRAllTypes.String, new[] { "http://example.com/profile2" }));

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.answer[x]")
            {
                ElementId = "Questionnaire.item.enableWhen:boolean.answer[x]",
            }.OfType(FHIRAllTypes.Boolean));
            return result;
        }

        private static StructureDefinition buildReferencedTypeAndProfileSlice()
        {
            var result = createTestSD(REFERENCEDTYPEANDPROFILESLICE, "ReferencedTypeAndProfileSliceTestcase",
                       "Testcase with a referenced type and profile slice on Questionnaire.item.enableWhen.answer[x]", FHIRAllTypes.Questionnaire);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var slicingIntro = new ElementDefinition("Questionnaire.item.enableWhen");

            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Profile, "answer.resolve()"),
                (ElementDefinition.DiscriminatorType.Type, "answer.resolve()"));
            cons.Add(slicingIntro);

            // Single slice (yeah, this is a test) is on the target of answer[Reference]
            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen")
            {
                ElementId = "Questionnaire.item.enableWhen:only1slice",
                SliceName = "Only1Slice"
            });

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.answer[x]")
            {
                ElementId = "Questionnaire.item.enableWhen:only1slice.answer[x]",
            }
            .OfReference(new[] { PATTERNSLICETESTCASE }));

            return result;
        }

        private static StructureDefinition buildExistSliceTestcase()
        {
            var result = createTestSD(EXISTSLICETESTCASE, "ExistSlicingTestcase",
                       "Testcase with an exist on Patient.name.family", FHIRAllTypes.Patient);

            var cons = result.Differential.Element;

            var slicingIntro = new ElementDefinition("Patient.name");
            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Exists, "family"));
            cons.Add(slicingIntro);

            // First slice, should slice on existence of name.family
            cons.Add(new ElementDefinition("Patient.name")
            {
                ElementId = "Patient.name:exists",
                SliceName = "Exists"
            });

            cons.Add(new ElementDefinition("Patient.name.family")
            {
                ElementId = "Patient.name:exists.family",
            }.Required());

            // Second slice, should slice on no-existence of name.family
            cons.Add(new ElementDefinition("Patient.name")
            {
                ElementId = "Patient.name:notexists",
                SliceName = "NotExists"
            });

            cons.Add(new ElementDefinition("Patient.name.family")
            {
                ElementId = "Patient.name:notexists.family",
            }.Prohibited());

            return result;
        }


        public static StructureDefinition buildResliceTestcase()
        {
            var result = createTestSD(RESLICETESTCASE, "ResliceTestcase",
           "Testcase with an slice + nested slice on Patient.telecom", FHIRAllTypes.Patient);

            var cons = result.Differential.Element;

            var slicingIntro = new ElementDefinition("Patient.telecom");

            // NB: discriminator-less matching is the parent slice
            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.OpenAtEnd,
                (ElementDefinition.DiscriminatorType.Value, "system"));

            cons.Add(slicingIntro);

            // First, slice into PHONE (not a discriminator!)
            cons.Add(new ElementDefinition("Patient.telecom")
            {
                ElementId = "Patient.telecom:phone",
                SliceName = "phone"
            }.Required(max: "2"));

            cons.Add(new ElementDefinition("Patient.telecom.system")
            {
                ElementId = "Patient.telecom:phone.system",
            }.Required().Value(new Code("phone")));

            // Now, the emails. A slice with Email (again, not a discriminator yet), re-sliced to account for system+use
            cons.Add(new ElementDefinition("Patient.telecom")
            {
                ElementId = "Patient.telecom:email",
                SliceName = "email"
            }
            .Required(min: 0, max: "1")
            .WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Value, "use")));

            cons.Add(new ElementDefinition("Patient.telecom.system")
            {
                ElementId = "Patient.telecom:email.system",
            }.Required().Value(new Code("email")));

            // A re-slice for Email + home
            cons.Add(new ElementDefinition("Patient.telecom")
            {
                ElementId = "Patient.telecom:email/home",
                SliceName = "email/home"
            }.Required(min: 0));

            cons.Add(new ElementDefinition("Patient.telecom.system")
            {
                ElementId = "Patient.telecom:email/home.system",
            }.Required().Value(new Code("email")));

            cons.Add(new ElementDefinition("Patient.telecom.use")
            {
                ElementId = "Patient.telecom:email/home.use",
            }.Required().Value(new Code("home")));

            // A re-slice for Email + work
            cons.Add(new ElementDefinition("Patient.telecom")
            {
                ElementId = "Patient.telecom:email/work",
                SliceName = "email/work"
            }.Required(min: 0));

            cons.Add(new ElementDefinition("Patient.telecom.system")
            {
                ElementId = "Patient.telecom:email/work.system",
            }.Required().Value(new Code("email")));

            cons.Add(new ElementDefinition("Patient.telecom.use")
            {
                ElementId = "Patient.telecom:email/work.use",
            }.Required().Value(new Code("work")));

            return result;
        }

        /*
      

        private static StructureDefinition buildOrganizationWithRegexConstraintOnName()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/MyOrganization", "My Organization",
                    "Test an organization with Name containing regex", FHIRAllTypes.Organization);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Organization").OfType(FHIRAllTypes.Organization));

            var nameDef = new ElementDefinition("Organization.name");
            nameDef.SetStringExtension("http://hl7.org/fhir/StructureDefinition/regex", "[A-Z].*");
            cons.Add(nameDef);

            return result;
        }


        private static StructureDefinition buildOrganizationWithRegexConstraintOnType()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/MyOrganization2", "My Organization",
                    "Test an organization with Name containing regex", FHIRAllTypes.Organization);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Organization").OfType(FHIRAllTypes.Organization));

            var nameDef = new ElementDefinition("Organization.name.value")
                .OfType(FHIRAllTypes.String);
            nameDef.Type.Single().SetStringExtension("http://hl7.org/fhir/StructureDefinition/regex", "[A-Z].*");
            // R4: [Primitive].value elements have no type code
            nameDef.Type.Single().Code = null;

            cons.Add(nameDef);

            return result;
        }

        private static StructureDefinition buildDutchPatient()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/DutchPatient", "Dutch Patient",
                    "Test Patient which requires an Identifier with either BSN or drivers license", FHIRAllTypes.Patient);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Patient").OfType(FHIRAllTypes.Patient));
            cons.Add(new ElementDefinition("Patient.identifier").Required(max: "*")
                        .OfType(FHIRAllTypes.Identifier, new[] { "http://validationtest.org/fhir/StructureDefinition/IdentifierWithBSN", "http://validationtest.org/fhir/StructureDefinition/IdentifierWithDL" }));

            return result;
        }

        private static StructureDefinition buildIdentifierWithBSN()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/IdentifierWithBSN", "BSN Identifier",
                    "Test Identifier which requires a BSN oid", FHIRAllTypes.Identifier);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Identifier").OfType(FHIRAllTypes.Identifier));
            cons.Add(new ElementDefinition("Identifier.system").Required().Value(fix: new FhirUri("urn:oid:2.16.840.1.113883.2.4.6.3")));
            cons.Add(new ElementDefinition("Identifier.period").Prohibited());
            cons.Add(new ElementDefinition("Identifier.assigner").Prohibited());

            return result;
        }

        private static StructureDefinition buildIdentifierWithDriversLicense()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/IdentifierWithDL", "Drivers license Identifier",
                    "Test Identifier which requires a drivers license oid", FHIRAllTypes.Identifier);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Identifier").OfType(FHIRAllTypes.Identifier));
            cons.Add(new ElementDefinition("Identifier.system").Required().Value(fix: new FhirUri("urn:oid:2.16.840.1.113883.2.4.6.12")));

            return result;
        }


        private static StructureDefinition buildQuestionnaireWithFixedType()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/QuestionnaireWithFixedType", "Fixed Questionnaire",
                    "Questionnaire with a fixed question type of 'decimal'", FHIRAllTypes.Questionnaire);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Questionnaire").OfType(FHIRAllTypes.Questionnaire));
            cons.Add(new ElementDefinition("Questionnaire.item.item.type").Value(fix: new Code("decimal")));

            return result;
        }

        private static StructureDefinition buildWeightQuantity()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/WeightQuantity", "Weight Quantity",
                    "Quantity which allows just kilograms", FHIRAllTypes.Quantity);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Quantity").OfType(FHIRAllTypes.Quantity));
            cons.Add(new ElementDefinition("Quantity.unit").Required().Value(fix: new FhirString("kg")));
            cons.Add(new ElementDefinition("Quantity.system").Required().Value(fix: new FhirUri("http://unitsofmeasure.org")));
            cons.Add(new ElementDefinition("Quantity.code").Required().Value(fix: new Code("kg")));

            return result;
        }

        private static StructureDefinition buildHeightQuantity()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/HeightQuantity", "Height Quantity",
                    "Quantity which allows just centimeters", FHIRAllTypes.Quantity);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Quantity").OfType(FHIRAllTypes.Quantity));
            cons.Add(new ElementDefinition("Quantity.unit").Required().Value(fix: new FhirString("cm")));
            cons.Add(new ElementDefinition("Quantity.system").Required().Value(fix: new FhirUri("http://unitsofmeasure.org")));
            cons.Add(new ElementDefinition("Quantity.code").Required().Value(fix: new Code("cm")));

            return result;
        }

        private static StructureDefinition buildWeightHeightObservation()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/WeightHeightObservation", "Weight/Height Observation",
                    "Observation with a choice of weight/height or another type of value", FHIRAllTypes.Observation);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Observation").OfType(FHIRAllTypes.Observation));
            cons.Add(new ElementDefinition("Observation.value[x]")
                .OfType(FHIRAllTypes.Quantity, new[] { "http://validationtest.org/fhir/StructureDefinition/WeightQuantity", "http://validationtest.org/fhir/StructureDefinition/HeightQuantity" })
                .OrType(FHIRAllTypes.String));

            return result;
        }


        private static StructureDefinition bundleWithSpecificEntries(string prefix)
        {
            var result = createTestSD($"http://validationtest.org/fhir/StructureDefinition/BundleWith{prefix}Entries", $"Bundle with specific {prefix} test entries",
                    $"Bundle with just Organization or {prefix} Patient entries", FHIRAllTypes.Bundle);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Bundle").OfType(FHIRAllTypes.Bundle));
            cons.Add(new ElementDefinition("Bundle.entry.resource")
                .OfType(FHIRAllTypes.Organization)
                .OrType(FHIRAllTypes.Patient, new[] { $"http://validationtest.org/fhir/StructureDefinition/PatientWith{prefix}Organization" }));

            return result;
        }

        private static StructureDefinition bundleWithConstrainedContained()
        {
            var result = createTestSD($"http://validationtest.org/fhir/StructureDefinition/BundleWithConstrainedContained",
                            $"Bundle with a constraint on the Bundle.entry.resource",
                    $"Bundle with a constraint on the Bundle.entry.resource", FHIRAllTypes.Bundle);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Bundle").OfType(FHIRAllTypes.Bundle));
            cons.Add(new ElementDefinition("Bundle.entry.resource.meta").Required());

            return result;
        }


        private static StructureDefinition patientWithSpecificOrganization(IEnumerable<ElementDefinition.AggregationMode> aggregation, string prefix)
        {
            var result = createTestSD($"http://validationtest.org/fhir/StructureDefinition/PatientWith{prefix}Organization", $"Patient with {prefix} managing organization",
                    $"Patient for which the managingOrganization reference is limited to {prefix} references", FHIRAllTypes.Patient);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Patient").OfType(FHIRAllTypes.Patient));
            cons.Add(new ElementDefinition("Patient.managingOrganization")
                .OfReference(new[] { (string)ModelInfo.CanonicalUriForFhirCoreType(FHIRAllTypes.Organization) }, aggregation));

            return result;
        }


        private static StructureDefinition buildParametersWithBoundParams()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/ParametersWithBoundParams", "Parameters with term binding on Params",
                    "Parameters resource where the parameter.value[x] is bound to a valueset", FHIRAllTypes.Parameters);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Parameters").OfType(FHIRAllTypes.Parameters));
            cons.Add(new ElementDefinition("Parameters.parameter.value[x]")
                    .WithBinding("http://hl7.org/fhir/ValueSet/data-absent-reason", BindingStrength.Required));

            return result;
        }

        private const string QUANTITY_WITH_UNLIMITED_ROOT_CARDINALITY_CANONICAL = "http://validationtest.org/fhir/StructureDefinition/QuantityWithUnlimitedRootCardinality";

        private static StructureDefinition buildQuantityWithUnlimitedRootCardinality()
        {
            var result = createTestSD(QUANTITY_WITH_UNLIMITED_ROOT_CARDINALITY_CANONICAL, "A Quantity with a root cardinality of 0..*",
                    "Parameters resource where the parameter.value[x] is bound to a valueset", FHIRAllTypes.Quantity);
            var cons = result.Differential.Element;

            var quantityRoot = new ElementDefinition("Quantity").OfType(FHIRAllTypes.Quantity);
            quantityRoot.Min = 0;
            quantityRoot.Max = "*";  // this is actually the case in all core classes as well.
            cons.Add(quantityRoot);

            return result;
        }

        private static StructureDefinition buildRangeWithLowAsAQuantityWithUnlimitedRootCardinality()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/RangeWithLowAsAQuantityWithUnlimitedRootCardinality",
                "Range referring to a profiled quantity",
                "Range that refers to a profiled quantity on its Range.low - this profiled Quantity has a 0..* root.",
                   FHIRAllTypes.Range);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Range").OfType(FHIRAllTypes.Range));
            cons.Add(new ElementDefinition("Range.low")
                .OfType(FHIRAllTypes.Quantity, profile: new[] { QUANTITY_WITH_UNLIMITED_ROOT_CARDINALITY_CANONICAL })
                .Required(min: 1, max: null));   // just set min to 1 and leave max out.

            return result;
        }

        private static StructureDefinition buildValueDescriminatorWithPattern()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/ValueDiscriminatorWithPattern", "A value discriminator including pattern[x]",
                    "Expresses a discriminator of type value, which expects pattern[x] to be considered part of it too.", FHIRAllTypes.Practitioner);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Practitioner").OfType(FHIRAllTypes.Practitioner));

            var slicingIntro = new ElementDefinition("Practitioner.identifier")
                .WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Pattern, "type"));

            cons.Add(slicingIntro);

            // Slice 1 - binds to FHIR's identifier-type (req) PLUS requires a local code translation
            cons.Add(new ElementDefinition("Practitioner.identifier")
            {
                ElementId = "Practitioner.identifier:slice1",
                SliceName = "slice1",
                Max = "1"
            });
            cons.Add(new ElementDefinition("Practitioner.identifier.type")
            {
                ElementId = "Practitioner.identifier:slice1.type",
                Pattern = new CodeableConcept("http://local-codes.nl/identifier-types", "ID-TYPE-1")
            }.WithBinding("http://hl7.org/fhir/ValueSet/identifier-type", BindingStrength.Required));

            // Slice 2 - binds to FHIR's identifier-type (req) PLUS requires another local code translation
            cons.Add(new ElementDefinition("Practitioner.identifier")
            {
                ElementId = "Practitioner.identifier:slice2",
                SliceName = "slice2",
                Max = "*"
            });
            cons.Add(new ElementDefinition("Practitioner.identifier.type")
            {
                ElementId = "Practitioner.identifier:slice2.type",
                Pattern = new CodeableConcept("http://local-codes.nl/identifier-types", "ID-TYPE-2")
            }.WithBinding("http://hl7.org/fhir/ValueSet/identifier-type", BindingStrength.Required));

            // Slice 3 - binds to FHIR's identifier-type (req), no other requirements
            cons.Add(new ElementDefinition("Practitioner.identifier")
            {
                ElementId = "Practitioner.identifier:slice3",
                SliceName = "slice3",
                Max = "*"
            });
            cons.Add(new ElementDefinition("Practitioner.identifier.type")
            {
                ElementId = "Practitioner.identifier:slice3.type",
            }.WithBinding("http://hl7.org/fhir/ValueSet/identifier-type", BindingStrength.Required));

            return result;
        }*/


        private static StructureDefinition createTestSD(string url, string name, string description, FHIRAllTypes constrainedType, string? baseUri = null)
        {
            var result = new StructureDefinition();

            result.Url = url;
            result.Name = name;
            result.Status = PublicationStatus.Draft;
            result.Description = new Markdown(description);
            result.FhirVersion = EnumUtility.ParseLiteral<FHIRVersion>(ModelInfo.Version);
            result.Derivation = StructureDefinition.TypeDerivationRule.Constraint;

            if (ModelInfo.IsKnownResource(constrainedType))
                result.Kind = StructureDefinition.StructureDefinitionKind.Resource;
            else if (ModelInfo.IsPrimitive(constrainedType))
                result.Kind = StructureDefinition.StructureDefinitionKind.PrimitiveType;
            else if (ModelInfo.IsDataType(constrainedType))
                result.Kind = StructureDefinition.StructureDefinitionKind.ComplexType;
            else
                result.Kind = StructureDefinition.StructureDefinitionKind.Logical;

            result.Type = constrainedType.GetLiteral();
            result.Abstract = false;

            if (baseUri == null)
                baseUri = ResourceIdentity.Core(constrainedType.GetLiteral()).ToString();

            result.BaseDefinition = baseUri;

            result.Differential = new StructureDefinition.DifferentialComponent();

            return result;
        }

        public Resource ResolveByCanonicalUri(string uri)
        {
            return TestProfiles.SingleOrDefault(p => p.Url == uri);
        }

        public Resource ResolveByUri(string uri)
        {
            return ResolveByCanonicalUri(uri);
        }

    }
}
