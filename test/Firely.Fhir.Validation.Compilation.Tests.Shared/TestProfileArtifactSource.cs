/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class TestProfileArtifactSource : IResourceResolver
    {
        public const string PATTERNSLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/PatternSliceTestcase";
        public const string VALUESLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcase";
        public const string VALUESLICETESTCASEOPEN = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcaseOpen";
        public const string VALUESLICETESTCASEWITHDEFAULT = "http://validationtest.org/fhir/StructureDefinition/ValueSliceTestcaseWithDefault";
        public const string DISCRIMINATORLESS = "http://validationtest.org/fhir/StructureDefinition/DiscriminatorlessTestcase";
        public const string TYPEANDPROFILESLICE = "http://validationtest.org/fhir/StructureDefinition/TypeAndProfileTestcase";
        public const string TYPERENAMESLICING = "http://validationtest.org/fhir/StructureDefinition/TypeRenameSlicing";
        public const string REFERENCEDTYPEANDPROFILESLICE = "http://validationtest.org/fhir/StructureDefinition/ReferencedTypeAndProfileTestcase";
        public const string SECONDARYTARGETREFSLICE = "http://validationtest.org/fhir/StructureDefinition/SecondaryTargetRefSlice";
        public const string EXISTSLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ExistSliceTestcase";
        public const string RESLICETESTCASE = "http://validationtest.org/fhir/StructureDefinition/ResliceTestcase";
        public const string INCOMPATIBLECARDINALITYTESTCASE = "http://validationtest.org/fhir/StructureDefinition/IncompatibleCardinalityTestcase";
        public const string PROFILEDBACKBONEANDCONTENTREF = "http://validationtest.org/fhir/StructureDefinition/ProfiledBackboneAndContentref";
        public const string PROFILEDOBSERVATIONSUBJECTREF = "http://validationtest.org/fhir/StructureDefinition/ProfiledObservation";

        public const string PROFILEDORG1 = "http://validationtest.org/fhir/StructureDefinition/ProfiledOrg1";
        public const string PROFILEDORG2 = "http://validationtest.org/fhir/StructureDefinition/ProfiledOrg2";
        public const string PROFILEDPROCEDURE = "http://validationtest.org/fhir/StructureDefinition/ProfiledProcedure";
        public const string PROFILEDFLAG = "http://validationtest.org/fhir/StructureDefinition/ProfiledFlag";

        public const string PROFILEDBOOL = "http://validationtest.org/fhir/StructureDefinition/booleanProfile";
        public const string PROFILEDSTRING = "http://validationtest.org/fhir/StructureDefinition/stringProfile";
        public const string PATIENTWITHPROFILEDREFS = "http://validationtest.org/fhir/StructureDefinition/PatientWithReferences";
        public const string BUNDLEWITHCONSTRAINEDCONTAINED = "http://validationtest.org/fhir/StructureDefinition/BundleWithConstrainedContained";


        public List<StructureDefinition> TestProfiles = new()
        {
            // The next two test cases should produce the same outcome, since value and pattern
            // discriminators have been merged (at least, in R5).
            buildValueOrPatternSliceTestcase(PATTERNSLICETESTCASE),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASE),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASEWITHDEFAULT),
            buildValueOrPatternSliceTestcase(VALUESLICETESTCASEOPEN),
            buildValueOrPatternSliceTestcase(DISCRIMINATORLESS),
            buildTypeAndProfileSlice(),
        //    buildTypeRenameSlicing(),
            buildReferencedTypeAndProfileSlice(),
            buildSliceWithSecondaryTargetReferenceDiscriminator(),
            buildExistSliceTestcase(),
            buildResliceTestcase(),
            buildIncompatibleCardinalityInIntro(),
            buildProfiledBackboneAndContentref(),
            buildObservationWithTargetProfilesAndChildDefs(),
            createTestSD(PROFILEDORG1, "NoopOrgProfile1", "A noop profile for an organization 1", FHIRAllTypes.Organization),
            createTestSD(PROFILEDORG2, "NoopOrgProfile2", "A noop profile for an organization 2", FHIRAllTypes.Organization),
            createTestSD(PROFILEDPROCEDURE, "NoopProcProfile", "A noop profile for a procedure", FHIRAllTypes.Procedure),
            buildFlagWithProfiledReferences(),
            createTestSD(PROFILEDSTRING, "NoopStringProfile", "A noop profile for a string", FHIRAllTypes.String),
            createTestSD(PROFILEDBOOL, "NoopBoolProfile", "A noop profile for a bool", FHIRAllTypes.Boolean),
            buildPatientWithProfiledReferences(),
            bundleWithConstrainedContained()
        };

        private static StructureDefinition buildFlagWithProfiledReferences()
        {
            var result = createTestSD(PROFILEDFLAG, "FlagWithProfiledReferences", "A flag profile that profiles its subject references", FHIRAllTypes.Flag);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Flag.subject").OfReference(new[] { PROFILEDORG1, PROFILEDORG2, PROFILEDPROCEDURE }));

            return result;
        }

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

        private static StructureDefinition buildTypeRenameSlicing()
        {
            var result = createTestSD(TYPERENAMESLICING, "TypeRenameSlicing",
                    "Testcase where a choice type gets sliced by using the shortcut type renaming, e.g. Observation.valueQuantity", FHIRAllTypes.Observation);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var slicingIntro = new ElementDefinition("Observation.valueQuantity");
            cons.Add(slicingIntro);

            // First slice is on question[string profile] and answer[String]
            cons.Add(new ElementDefinition("Observation.valueQuantity.value").OfType(FHIRAllTypes.Decimal));

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

            // First slice is on question[string profile] and answer[String]
            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen")
            {
                ElementId = "Questionnaire.item.enableWhen:string",
                SliceName = "string"
            });

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.question")
            {
                ElementId = "Questionnaire.item.enableWhen:string.question",
            }.OfTypeWithProfiles(FHIRAllTypes.String, new[] { PROFILEDSTRING }));

            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen.answer[x]")
            {
                ElementId = "Questionnaire.item.enableWhen:string.answer[x]",
            }.OfTypeWithProfiles(FHIRAllTypes.String));

            // Second slice is on answer[Boolean], but no profile set on question
            cons.Add(new ElementDefinition("Questionnaire.item.enableWhen")
            {
                ElementId = "Questionnaire.item.enableWhen:boolean",
                SliceName = "boolean"
            });

            //It's unclear whether having one of the two discriminating values
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

        private static StructureDefinition buildSliceWithSecondaryTargetReferenceDiscriminator()
        {
            var result = createTestSD(SECONDARYTARGETREFSLICE, "SecondaryTargetReferenceSliceTestcase",
                       "Testcase with two type slices, where the second discriminator for a reference and is not applicable to all slices.", FHIRAllTypes.Communication);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var slicingIntro = new ElementDefinition("Communication.payload");

            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
                (ElementDefinition.DiscriminatorType.Type, "content"),
                (ElementDefinition.DiscriminatorType.Type, "content.ofType(Reference).resolve()"));
            cons.Add(slicingIntro);

#if R5
            // Intro slice child content[x]
            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            .OrType(FHIRAllTypes.Attachment)
            .OrReferenceWithProfiles(
                new[] { "http://hl7.org/fhir/StructureDefinition/DocumentReference",
                "http://hl7.org/fhir/StructureDefinition/Task" }));

            // Slice 1 ==========================
            cons.Add(new ElementDefinition("Communication.payload")
            {
                ElementId = "Communication.payload:Attachment",
                SliceName = "Attachment"
            });

            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            {
                ElementId = "Communication.payload:Attachment.content[x]",
            }.OfType(FHIRAllTypes.Attachment));
#else
            // Intro slice child content[x]
            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            .OrType(FHIRAllTypes.String)
            .OrReferenceWithProfiles(
                new[] { "http://hl7.org/fhir/StructureDefinition/DocumentReference",
                "http://hl7.org/fhir/StructureDefinition/Task" }));

            // Slice 1 ==========================
            cons.Add(new ElementDefinition("Communication.payload")
            {
                ElementId = "Communication.payload:String",
                SliceName = "String"
            });

            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            {
                ElementId = "Communication.payload:String.content[x]",
            }.OfType(FHIRAllTypes.String));
#endif

            // Slice 2 ===========================
            cons.Add(new ElementDefinition("Communication.payload")
            {
                ElementId = "Communication.payload:DocumentReference",
                SliceName = "DocumentReference"
            }.Required(max: "20"));

            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            {
                ElementId = "Communication.payload:DocumentReference.content[x]",
            }.OfReference("http://hl7.org/fhir/StructureDefinition/DocumentReference"));

            // Slice 3 ===========================
            cons.Add(new ElementDefinition("Communication.payload")
            {
                ElementId = "Communication.payload:Task",
                SliceName = "Task"
            }.Required(max: "11"));

            cons.Add(new ElementDefinition("Communication.payload.content[x]")
            {
                ElementId = "Communication.payload:Task.content[x]",
            }.OfReference("http://hl7.org/fhir/StructureDefinition/Task").Required());


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


        private static StructureDefinition buildIncompatibleCardinalityInIntro()
        {
            var result = createTestSD(INCOMPATIBLECARDINALITYTESTCASE, "IncompatibleCardinalityInIntro",
                       "Testcase with an intro slice with a cardinality that is less strict than the slices", FHIRAllTypes.Patient);

            var cons = result.Differential.Element;

            var slicingIntro = new ElementDefinition("Patient.identifier");
            slicingIntro.WithSlicingIntro(ElementDefinition.SlicingRules.Closed,
               (ElementDefinition.DiscriminatorType.Value, "system"));
            slicingIntro.Required(min: 0, max: "1");
            cons.Add(slicingIntro);

            // First slice, should slice on the "fixed" of system
            // AND demand a minimum cardinality of 1 (which is incompatible
            // with the intro cardinality.
            cons.Add(new ElementDefinition("Patient.identifier")
            {
                ElementId = "Patient.identifier:fixed",
                SliceName = "Fixed1"
            }.Required(min: 1, max: "1"));

            cons.Add(new ElementDefinition("Patient.identifier.system")
            {
                ElementId = "Patient.identifier:fixed.system",
            }.Value(fix: new FhirUri("http://example.com/some-bsn-uri")));

            // Second slice, should slice on the "fixed" of system
            // AND demand a minimum cardinality of 1 (which is incompatible
            // with the intro cardinality.
            cons.Add(new ElementDefinition("Patient.identifier")
            {
                ElementId = "Patient.identifier:fixed",
                SliceName = "Fixed2"
            }.Required(min: 1, max: "1"));

            cons.Add(new ElementDefinition("Patient.identifier.system")
            {
                ElementId = "Patient.identifier:fixed.system",
            }.Value(fix: new FhirUri("http://example.com/another-bsn-uri")));



            return result;
        }

        private static StructureDefinition buildProfiledBackboneAndContentref()
        {
            var result = createTestSD(PROFILEDBACKBONEANDCONTENTREF, "ProfiledBackboneAndContentref",
                       "Testcase with a cardinality constraint on both Questionnaire.item and Questionnaire.item.item", FHIRAllTypes.Questionnaire);

            // Define a slice based on a "value" type discriminator
            var cons = result.Differential.Element;
            var item = new ElementDefinition("Questionnaire.item").Required(1, "100");
            cons.Add(item);

            var itemItem = new ElementDefinition("Questionnaire.item.item").Required(5, "10");
            cons.Add(itemItem);

            return result;
        }

        private static StructureDefinition buildPatientWithProfiledReferences()
        {
            var result = createTestSD(PATIENTWITHPROFILEDREFS, "Patient with References",
                    "Test Patient which has a profiled managing organization", FHIRAllTypes.Patient);
            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Patient").OfType(FHIRAllTypes.Patient));
            cons.Add(new ElementDefinition("Patient.managingOrganization").OfReference(PROFILEDORG2));
            return result;
        }

        private static StructureDefinition buildObservationWithTargetProfilesAndChildDefs()
        {
            var result = createTestSD(PROFILEDOBSERVATIONSUBJECTREF, "Observation-issue-1654",
                "Observation with targetprofile on subject and children definition under subject as well", FHIRAllTypes.Observation);

            var cons = result.Differential.Element;
            cons.Add(new ElementDefinition("Observation.subject")
            {
                ElementId = "Observation.subject",
            }.OfReference(targetProfiles: new[] { PATIENTWITHPROFILEDREFS, Canonical.ForCoreType("Patient").ToString() }));

            cons.Add(new ElementDefinition("Observation.subject.display")
            {
                ElementId = "Observation.subject.display",
                MaxLength = 10
            });

            return result;
        }

        private static StructureDefinition bundleWithConstrainedContained()
        {
            var result = createTestSD(BUNDLEWITHCONSTRAINEDCONTAINED,
                            $"Bundle with a constraint on the Bundle.entry.resource",
                    $"Bundle with a constraint on the Bundle.entry.resource", FHIRAllTypes.Bundle);

            var cons = result.Differential.Element;

            cons.Add(new ElementDefinition("Bundle").OfType(FHIRAllTypes.Bundle));
            cons.Add(new ElementDefinition("Bundle.entry.resource.meta").Required());

            return result;
        }

        private static StructureDefinition createTestSD(string url, string name, string description, FHIRAllTypes constrainedType, string? baseUri = null)
        {
            var result = new StructureDefinition
            {
                Url = url,
                Id = name,
                Name = name,
                Status = PublicationStatus.Draft,
                Description = new Markdown(description),
#if STU3
                FhirVersion = ModelInfo.Version,
#else
                FhirVersion = EnumUtility.ParseLiteral<FHIRVersion>(ModelInfo.Version),
#endif
                Derivation = StructureDefinition.TypeDerivationRule.Constraint
            };

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

            baseUri ??= ResourceIdentity.Core(constrainedType.GetLiteral()).ToString();

            result.BaseDefinition = baseUri;

            result.Differential = new StructureDefinition.DifferentialComponent();

            return result;
        }

        public Resource? ResolveByCanonicalUri(string uri)
        {
            return TestProfiles.SingleOrDefault(p => p.Url == uri);
        }

        public Resource? ResolveByUri(string uri)
        {
            return ResolveByCanonicalUri(uri);
        }

    }

    public static class CommonExtension
    {
        public static ElementDefinition OfReference(this ElementDefinition ed, IEnumerable<string> targetProfiles)
        {
#if STU3
            ed.Type.Clear();
            foreach (var targetProfile in targetProfiles)
            {
                ed.OrReference(targetProfile);
            }
            return ed;
#else
            return ed.OfReference(targetProfiles, null, null);
#endif
        }

        public static ElementDefinition OfTypeWithProfiles(this ElementDefinition ed, FHIRAllTypes type, IEnumerable<string>? profiles = null)
        {
#if STU3
            ed.Type.Clear();
            if (profiles?.Any() == true)
            {
                foreach (var profile in profiles)
                {
                    ed.OfType(type, profile);
                }
            }
            else
            {
                ed.OfType(type);
            }
            return ed;
#else
            return ed.OfType(type, profiles);
#endif
        }

        public static ElementDefinition OrTypeWithProfiles(this ElementDefinition ed, FHIRAllTypes type, IEnumerable<string>? profiles = null)
        {
#if STU3
            if (profiles?.Any() == true)
            {
                foreach (var profile in profiles)
                {
                    ed.OrType(type, profile);
                }
            }
            else
            {
                ed.OrType(type);
            }
            return ed;
#else
            return ed.OrType(type, profiles);
#endif
        }

        public static ElementDefinition OrReferenceWithProfiles(this ElementDefinition ed, IEnumerable<string> targetProfiles)
        {
#if STU3
            foreach (var targetProfile in targetProfiles)
            {
                ed.OrReference(targetProfile);
            }

            return ed;
#else
            return ed.OrReference(targetProfiles);
#endif
        }
    }

}
