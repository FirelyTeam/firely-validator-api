using Firely.Fhir.Packages;
using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Compilation.Tests;

public class TestContext
{
    public FhirPackageSource PackageResolver { get; }

    public TestContext(params string[] packageNames)
    {
        const string packageServer = "https://packages.simplifier.net";

        PackageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
    }
}