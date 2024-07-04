/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Firely.Fhir.Validation.Compilation.R4.Tests;

[TestClass]
public class Issue322
{
    [TestMethod]
    public void TestCreateSchema()
    {
        var package1 = "hl7.fhir.us.carin-bb@1.1.0";
        FhirPackageSource npm = new(ModelInfo.ModelInspector, "https://packages.simplifier.net", [package1]);
        DirectorySource issue322 = new(Path.Combine("TestData", "Issue-322", "conformance"));
        var multi = new MultiResolver(issue322, npm);
        SnapshotSource source = new(new CachedResolver(multi));
        var ts = new LocalTerminologyService(source);

        var resolver = new StructureDefinitionToElementSchemaResolver(source);
        var schema =
            resolver.GetSchema(
                "https://ncqa.org/fhir/StructureDefinition/hedis-core-explanationofbenefit-outpatient-institutional");
        schema.Should().NotBeNull();

        // Just making sure that actually validating against this schema (it's mentioned in the instance) doesn't throw an exception
        var eobXml = File.ReadAllText(Path.Combine("TestData", "Issue-322", "data", "test-eob.xml"));
        var eob = FhirSerializationEngineFactory.Ostrich(ModelInfo.ModelInspector).DeserializeFromXml(eobXml);

        var v = new Validator(source, ts, null, null);
        var result = v.Validate(eob);
        result.Success.Should().BeFalse(); // there are validation errors, but no Exceptions about compilation errors

        //schema.Validate(eob.ToTypedElement(ModelInfo.ModelInspector));
    }
}