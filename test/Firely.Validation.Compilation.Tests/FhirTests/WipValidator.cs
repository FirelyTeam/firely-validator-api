using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class WipValidator : ITestValidator
    {
        private readonly string[] _unsupportedTests = new[]
        {
            // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
            "cda/example", "cda/example-no-styles",
        };

        private readonly IElementSchemaResolver _schemaResolver;
        private readonly IAsyncResourceResolver? _resourceResolver;
        private readonly Stopwatch _stopWatch;

        public WipValidator(IElementSchemaResolver schemaResolver, IAsyncResourceResolver? resolver = null, Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
            _schemaResolver = schemaResolver;
            _resourceResolver = resolver;
        }

        public string Name => "Wip";

        public string[] UnvalidatableTests => _unsupportedTests;

        public bool CannotValidateTest(TestCase c) => UnvalidatableTests.Contains(c.Name);
        public ExpectedResult? GetExpectedResults(IValidatorEnginesResults engine) => engine.FirelySDKWip;
        public void SetExpectedResults(IValidatorEnginesResults engine, ExpectedResult result) => engine.FirelySDKWip = result;

        /// <summary>
        /// Validator engine based in this solution: the work in progress (wip) validator
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<OperationOutcome> Validate(ITypedElement instance, IResourceResolver resolver, string? profile = null)
        {
            var outcome = new OperationOutcome();
            List<ResultAssertion> result = new();

            // resolver of class has priority over the incoming resolver from this function
            var asyncResolver = _resourceResolver ?? resolver.AsAsync();

            foreach (var profileUri in getProfiles(instance, profile))
            {
                result.Add(await validate(instance, profileUri));
            }

            outcome.Add(ResultAssertion.FromEvidence(result).ToOperationOutcome());
            return outcome;

            async Task<ResultAssertion> validate(ITypedElement typedElement, string canonicalProfile)
            {
                try
                {
                    // _schemaResolver of class has priority 
                    var schemaResolver = new MultiElementSchemaResolver(_schemaResolver, StructureDefinitionToElementSchemaResolver.CreatedCached(asyncResolver));
                    var schema = await schemaResolver.GetSchema(canonicalProfile);
                    var validationContext = new ValidationContext(schemaResolver,
                            new TerminologyServiceAdapter(new LocalTerminologyService(asyncResolver)))
                    {
                        ExternalReferenceResolver = async u => (await asyncResolver.ResolveByUriAsync(u))?.ToTypedElement(),
                        // IncludeFilter = Settings.SkipConstraintValidation ? (Func<IAssertion, bool>)(a => !(a is FhirPathAssertion)) : (Func<IAssertion, bool>)null,
                        // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                        // should be removed from STU3/R4 once we get the new normative version
                        // of FP up, which could do comparisons between quantities.
                        ExcludeFilter = a => (a is FhirPathValidator fhirPathAssertion && fhirPathAssertion.Key == "rng-2"),
                    };

                    _stopWatch.Start();
                    var result = await schema!.Validate(typedElement, validationContext);
                    _stopWatch.Stop();
                    return result;
                }
                catch (Exception ex)
                {
                    return new ResultAssertion(ValidationResult.Failure, new IssueAssertion(-1, "", ex.Message, IssueSeverity.Error));
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

                var instanceType = ModelInfo.CanonicalUriForFhirCoreType(node.InstanceType).Value;
                if (instanceType is not null)
                {
                    yield return instanceType;
                }
            }
        }
    }
}
