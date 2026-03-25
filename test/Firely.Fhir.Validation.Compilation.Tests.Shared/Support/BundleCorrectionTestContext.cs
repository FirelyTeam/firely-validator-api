using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;
using Task = System.Threading.Tasks.Task;

namespace Firely.Fhir.Validation.Compilation.Tests;

public class BundleCorrectionTestContext : TestContext
{
    public MultiResolver ProjectResolver { get; }

    public const string BUNDLE_URL = "http://hl7.org/fhir/StructureDefinition/Bundle";
    public const string DERIVED_BUNDLE_URL = "http://example.org/fhir/StructureDefinition/DerivedBundle";
    public const string CONSTRAINT_KEY_1 = "tst-1";

    public BundleCorrectionTestContext(params string[] packageNames) : base(packageNames)
    {
        ProjectResolver = new MultiResolver(PackageResolver, new InMemoryResourceResolver(createDerivedBundleStructureDefinition()));
    }

    public async Task RunTest(IAsyncResourceResolver correctingResolver, List<string> missingConstraints)
    {
        var bundle = await correctingResolver.ResolveByCanonicalUriAsync(BUNDLE_URL);

        // Bundle should contain the missing constraints in both snapshot and differential
        check(bundle, missingConstraints, true);
        
        var snapshotSource = new SnapshotSource(correctingResolver); // Make sure a snapshot is present
        var derivedBundle = await snapshotSource.ResolveByCanonicalUriAsync(DERIVED_BUNDLE_URL);

        // Derived bundle should only contain the missing constraints in snapshot (as they are inherited) but not in the differential (as they are corrected)
        check(derivedBundle, missingConstraints, false);

        // Derived bundle should contain the test constraint in both snapshot and differential
        check(derivedBundle, [CONSTRAINT_KEY_1], true);
    }

    private static void check(Resource? resource, List<string> constraints, bool diffShouldContainConstraints)
    {
        resource.Should().NotBeNull();
        resource.Should().BeOfType<StructureDefinition>();

        var sd = (StructureDefinition)resource;

        sd.Snapshot.Should().NotBeNull();
        sd.Snapshot.Element.Should().NotBeEmpty();

        // Snapshot should always contain the constraints, even for the derived bundle
        check(sd.Snapshot.Element, constraints, true);

        sd.Differential.Should().NotBeNull();
        sd.Differential.Element.Should().NotBeEmpty();

        check(sd.Differential.Element, constraints, diffShouldContainConstraints);
    }

    private static void check(List<ElementDefinition> elements, List<string> constraints, bool shouldContainConstraints)
    {
        var bundleElements = elements.Where(e => e.ElementId == "Bundle").ToList();
        bundleElements.Should().HaveCount(1);

        var bundleElement = bundleElements[0];

        if (shouldContainConstraints)
        {
            foreach (var key in constraints)
                bundleElement.Constraint.Should().Contain(c => c.Key == key);
        }
        else
        {
            foreach (var key in constraints)
                bundleElement.Constraint.Should().NotContain(c => c.Key == key);
        }
    }

    private static StructureDefinition createDerivedBundleStructureDefinition()
    {
        var sd = new StructureDefinition
        {
            Url = DERIVED_BUNDLE_URL,
            Name = "DerivedBundle",
            Status = PublicationStatus.Draft,
            Kind = StructureDefinition.StructureDefinitionKind.Resource,
#if STU3
            FhirVersion = ModelInfo.Version,
#else
            FhirVersion = EnumUtility.ParseLiteral<FHIRVersion>(ModelInfo.Version),
#endif
            Abstract = false,
            Type = "Bundle",
            Derivation = StructureDefinition.TypeDerivationRule.Constraint,
            BaseDefinition = BUNDLE_URL,
            Differential = new StructureDefinition.DifferentialComponent
            {
                Element =
                [
                    new ElementDefinition
                    {
                        ElementId = "Bundle",
                        Path = "Bundle",
                        Constraint =
                        [
                            new ConstraintComponent
                            {
                                Key = CONSTRAINT_KEY_1,
                                Expression = "true",
                                Severity = ConstraintSeverity.Warning,
                                Human = "This is a test constraint"
                            }
                        ]
                    }
                ]
            }
        };

        return sd;
    }
}