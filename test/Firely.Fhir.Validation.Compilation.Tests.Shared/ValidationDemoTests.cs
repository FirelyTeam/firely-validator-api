#if !R5

/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using M = Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ValidationDemoTests
    {
#if R4
        private const FhirRelease RELEASE = FhirRelease.R4;
#elif STU3
        private const FhirRelease RELEASE = FhirRelease.STU3;
#endif
        private static readonly string TEST_DIRECTORY = Path.GetFullPath(Path.Combine("TestData", "DocumentComposition"));

        [TestMethod]
        public void RunValidateOnDemoBundle()
        {
            var result = runValidation("MainBundle.bundle.xml");
            result.Success.Should().BeTrue();
        }

        [TestMethod]
        public void RunValidateOnErrorDemoBundle()
        {
            var result = runValidation("MainBundle-with-errors.bundle.xml");
#if R4
            var checkedResult = """
                Overall result: FAILURE (13 errors and 1 warnings)
                [ERROR] Instance failed constraint bdl-1 "total only when a search or history" (at Bundle, element Bundle(http://example.org/StructureDefinition/DocumentBundle))
                [ERROR] Instance count is 1, which is not within the specified cardinality of 0..0 (at Bundle.total, element Bundle(http://example.org/StructureDefinition/DocumentBundle).total)
                [ERROR] Encountered a reference (http://example.org/fhir/Practitioner/Hippocrates) of kind 'Referenced', which is not one of the allowed kinds (Bundled). (at Bundle.entry[0].resource[0].author[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).author)
                [ERROR] Element does not validate against any of the expected profiles (http://example.org/StructureDefinition/WeightQuantity, http://example.org/StructureDefinition/HeightQuantity). (at Bundle.entry[1].resource[0].valueQuantity[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity])
                [ERROR] Value '75600' is larger than 200) (at Bundle.entry[1].resource[0].valueQuantity[0].value[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).value)
                [ERROR] Value 'http://unitsofmeasurex.org' is not exactly equal to fixed value 'http://unitsofmeasure.org' (at Bundle.entry[1].resource[0].valueQuantity[0].system[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).system)
                [ERROR] Value 'g' is not exactly equal to fixed value 'kg' (at Bundle.entry[1].resource[0].valueQuantity[0].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).code)
                [ERROR] Value '75600' is larger than 350) (at Bundle.entry[1].resource[0].valueQuantity[0].value[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity]->Quantity(http://example.org/StructureDefinition/HeightQuantity).value)
                [ERROR] Value 'g' is not exactly equal to fixed value 'cm' (at Bundle.entry[1].resource[0].valueQuantity[0].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][Quantity]->Quantity(http://example.org/StructureDefinition/HeightQuantity).code)
                [ERROR] Element is of type 'boolean', which is not one of the allowed choice types ('string','Quantity') (at Bundle.entry[2].resource[0].valueBoolean[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation.value[x][@default])
                [WARNING] Code '10154-3' from system 'http://loinc.org' has incorrect display 'Hoofdklacht', should be 'Chief complaint' (at Bundle.entry[0].resource[0].section[2].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.code)
                [ERROR] Value 'Misc internal annotations' is too long (maximum length is 20 (at Bundle.entry[0].resource[0].section[4].title[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.title)
                [ERROR] Code 'internal notes' from system 'http://example.org/fhir/demo-section-titles' does not exist in the value set 'MainBundle Section title codes' (http://example.org/ValueSet/SectionTitles) (at Bundle.entry[0].resource[0].section[4].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.code)
                [ERROR] Instance count is 2, which is not within the specified cardinality of 1..1 (at Bundle.entry[0].resource[0].section, element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section[vitalSigns])
                """.Trim('\t', '\n', '\r', ' ');
#elif STU3
            var checkedResult = """
            Overall result: FAILURE (13 errors and 1 warnings)
            [ERROR] Instance failed constraint bdl-1 "total only when a search or history" (at Bundle, element Bundle(http://example.org/StructureDefinition/DocumentBundle))
            [ERROR] Instance count is 1, which is not within the specified cardinality of 0..0 (at Bundle.total, element Bundle(http://example.org/StructureDefinition/DocumentBundle).total)
            [ERROR] Encountered a reference (http://example.org/fhir/Practitioner/Hippocrates) of kind 'Referenced', which is not one of the allowed kinds (Bundled). (at Bundle.entry[0].resource[0].author[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).author)
            [ERROR] Element does not validate against any of the expected profiles (http://example.org/StructureDefinition/WeightQuantity, http://example.org/StructureDefinition/HeightQuantity). (at Bundle.entry[1].resource[0].valueQuantity[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity])
            [ERROR] Value '75600' is larger than 200) (at Bundle.entry[1].resource[0].valueQuantity[0].value[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).value)
            [ERROR] Value 'http://unitsofmeasurex.org' is not exactly equal to fixed value 'http://unitsofmeasure.org' (at Bundle.entry[1].resource[0].valueQuantity[0].system[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).system)
            [ERROR] Value 'g' is not exactly equal to fixed value 'kg' (at Bundle.entry[1].resource[0].valueQuantity[0].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity]->Quantity(http://example.org/StructureDefinition/WeightQuantity).code)
            [ERROR] Value '75600' is larger than 350) (at Bundle.entry[1].resource[0].valueQuantity[0].value[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity]->Quantity(http://example.org/StructureDefinition/HeightQuantity).value)
            [ERROR] Value 'g' is not exactly equal to fixed value 'cm' (at Bundle.entry[1].resource[0].valueQuantity[0].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][Quantity]->Quantity(http://example.org/StructureDefinition/HeightQuantity).code)
            [ERROR] Element is of type 'boolean', which is not one of the allowed choice types ('string','Quantity') (at Bundle.entry[2].resource[0].valueBoolean[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.entry->Observation(http://example.org/StructureDefinition/WeightHeightObservation).value[x][@default])
            [WARNING] Code '10154-3' from system 'http://loinc.org' has incorrect display 'Hoofdklacht', should be 'Chief complaint' (at Bundle.entry[0].resource[0].section[2].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.code)
            [ERROR] Value 'Misc internal annotations' is too long (maximum length is 20 (at Bundle.entry[0].resource[0].section[4].title[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.title)
            [ERROR] Code 'internal notes' from system 'http://example.org/fhir/demo-section-titles' does not exist in the value set 'MainBundle Section title codes' (http://example.org/ValueSet/SectionTitles) (at Bundle.entry[0].resource[0].section[4].code[0], element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section.code)
            [ERROR] Instance count is 2, which is not within the specified cardinality of 1..1 (at Bundle.entry[0].resource[0].section, element Bundle(http://example.org/StructureDefinition/DocumentBundle).entry.resource->Composition(http://example.org/StructureDefinition/DocumentComposition).section[vitalSigns])
            """.Trim('\t', '\n', '\r', ' ');
#endif
            result.Success.Should().BeFalse();
            result.Errors.Should().Be(13);
            result.Warnings.Should().Be(1);
            var resultString = result.ToString().Trim('\t', '\n', '\r', ' ');
            resultString.Should().Be(checkedResult);
        }

        private static M.OperationOutcome runValidation(string instanceFile)
        {
            // This simulates the setup in the Firely Validation Demo project (https://github.com/FirelyTeam/Firely.Fhir.ValidationDemo),
            // so encountered issues in that demo can be debugged. It also means that we need to periodically check if the demo project
            // has been updated, and if so, update this test.
            var packageSource = FhirPackageSource.CreateCorePackageSource(M.ModelInfo.ModelInspector, RELEASE, "https://packages.simplifier.net");
            var testFilesResolver = new DirectorySource(TEST_DIRECTORY);
            var combinedSource = new MultiResolver(testFilesResolver, packageSource);
            var profileSource = new SnapshotSource(new CachedResolver(combinedSource));
            var terminologySource = new LocalTerminologyService(profileSource);
            var pd = new DirectoryInfo(TEST_DIRECTORY);
            var externalReferenceResolver = new FileBasedExternalReferenceResolver(pd);
            var validator = new Validator(profileSource, terminologySource, externalReferenceResolver);

            var xmlPocoDeserializer = new FhirXmlPocoDeserializer();
            var instanceText = File.ReadAllText(Path.Combine(TEST_DIRECTORY, instanceFile));
            var poco = xmlPocoDeserializer.DeserializeResource(instanceText);
            var result = validator.Validate(poco);
            return result;
        }
    }

    internal class FileBasedExternalReferenceResolver(DirectoryInfo baseDirectory) : IExternalReferenceResolver
    {
        private FhirXmlPocoDeserializer XmlPocoDeserializer { get; } = new FhirXmlPocoDeserializer();

        public DirectoryInfo BaseDirectory { get; private set; } = baseDirectory;

        public bool Enabled { get; set; } = true;

        public Task<object?> ResolveAsync(string reference)
        {
            if (!Enabled) return Task.FromResult<object?>(null);

            // Now, for our examples we've used the convention that the file can be found in the
            // example directory, with the name <id>.<type>.xml, so let's try to get that file.
            var identity = new ResourceIdentity(reference);
            var filename = $"{identity.Id}.{identity.ResourceType}.xml";
            var path = Path.Combine(BaseDirectory.FullName, filename);

            if (File.Exists(path))
            {
                var xml = File.ReadAllText(path);

                // Note, this will throw if the file is not really FHIR xml
                var poco = XmlPocoDeserializer.DeserializeResource(xml);

                return Task.FromResult<object?>(poco);
            }
            else
            {
                // Unsuccessful
                return Task.FromResult<object?>(null);
            }

        }
    }
}

#endif