/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Specification.Source;
using System.Threading.Tasks;
using M=Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Compilation.Tests;

[TestClass]
public class ExternalPackageCompilationTests
{
    [TestMethod]
    public async Task TestCRDPatientProfileSlincingOnIdentifier()
    {
        var pkgSource = new CachedResolver(new FhirPackageSource(M.ModelInfo.ModelInspector,
            "https://packages.simplifier.net", ["hl7.fhir.us.davinci-crd@2.0.1"]));

        var pp = await pkgSource.FindStructureDefinitionAsync(
            "http://hl7.org/fhir/us/davinci-crd/StructureDefinition/profile-patient");
        pp.Should().NotBeNull();

        var builder = new SchemaBuilder(pkgSource, [new StandardBuilders(pkgSource)]);
        builder.BuildSchema(pp);
    }

}