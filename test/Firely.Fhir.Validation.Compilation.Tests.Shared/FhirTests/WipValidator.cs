using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class WipValidator : ITestValidator
    {
        private readonly string[] _unsupportedTests = new[]
        {
            // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
            "cda/example", "cda/example-no-styles",
            // do not run an Empty testcase
            ValidationManifestDataSourceAttribute.EMPTY_TESTCASE_NAME
        };

        private static readonly IResourceResolver BASE_RESOLVER = new CachedResolver(new StructureDefinitionCorrectionsResolver(ZipSource.CreateValidationSource()));
        private static readonly IElementSchemaResolver SCHEMA_RESOLVER = StructureDefinitionToElementSchemaResolver.CreatedCached(BASE_RESOLVER.AsAsync());
        private readonly Stopwatch _stopWatch;

        public WipValidator(Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
        }

        public static ITestValidator Create() => new WipValidator();

        public string Name => "Wip";

        public string[] UnvalidatableTests => _unsupportedTests;

        public bool CannotValidateTest(TestCase c) => UnvalidatableTests.Contains(c.Name);
        public ExpectedResult? GetExpectedResults(IValidatorEnginesResults engine) => engine.FirelySDKWip;
        public void SetExpectedResults(IValidatorEnginesResults engine, ExpectedResult result) => engine.FirelySDKWip = result;

        /// <summary>
        /// Validator engine based in this solution: the work in progress (wip) validator
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IResourceResolver? resolver, string? profile = null)
        {
            var outcome = new OperationOutcome();
            List<ResultReport> result = new();

            var asyncResolver = (resolver is null ? BASE_RESOLVER : new SnapshotSource(new MultiResolver(BASE_RESOLVER, resolver))).AsAsync();

            foreach (var profileUri in getProfiles(instance, profile))
            {
                result.Add(validate(instance, profileUri));
            }

            outcome.Add(ResultReport.Combine(result)
               .CleanUp()
               .ToOperationOutcome());
            return outcome;

            ResultReport validate(ITypedElement typedElement, string canonicalProfile)
            {
                try
                {
                    var schemaResolver = new MultiElementSchemaResolver(SCHEMA_RESOLVER, StructureDefinitionToElementSchemaResolver.CreatedCached(asyncResolver));
                    var schema = schemaResolver.GetSchema(canonicalProfile);
                    var constraintsToBeIgnored = new string[] { "rng-2", "dom-6" };
                    var validationContext = new ValidationContext(schemaResolver, new LocalTerminologyService(asyncResolver))
                    {
                        ResolveExternalReference = (u, _) => TaskHelper.Await(() => asyncResolver.ResolveByUriAsync(u))?.ToTypedElement(),
                        // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                        // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                        // should be removed from STU3/R4 once we get the new normative version
                        // of FP up, which could do comparisons between quantities.
                        // 2022-01-19 MS: added best practice constraint "dom-6" to be ignored, which checks if a resource has a narrative.
                        ExcludeFilters = new Predicate<IAssertion>[] { a => a is FhirPathValidator fhirPathAssertion && constraintsToBeIgnored.Contains(fhirPathAssertion.Key) }
                    };

                    _stopWatch.Start();
                    var result = schema!.Validate(typedElement, validationContext);
                    _stopWatch.Stop();
                    return result;
                }
                catch (Exception ex)
                {
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-1, ex.Message, IssueSeverity.Error));
                }
            }

            IEnumerable<string> getProfiles(ITypedElement node, string? profile = null)
            {
                foreach (var item in node.Children("meta").Children("profile").Select(p => p.Value).Cast<string>())
                {
                    yield return item;
                }
                if (profile is not null)
                {
                    yield return profile;
                }

                var instanceType = ModelInfo.CanonicalUriForFhirCoreType(node.InstanceType);
                if (instanceType is not null)
                {
                    yield return instanceType!;
                }
            }
        }
    }
}
