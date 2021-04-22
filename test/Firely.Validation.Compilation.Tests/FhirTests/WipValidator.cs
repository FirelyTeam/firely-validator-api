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
        private readonly IElementSchemaResolver _schemaResolver;
        private readonly IAsyncResourceResolver? _resourceResolver;
        private readonly Stopwatch _stopWatch;

        public WipValidator(IElementSchemaResolver schemaResolver, IAsyncResourceResolver? resolver = null, Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
            _schemaResolver = schemaResolver;
            _resourceResolver = resolver;
        }

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
            var result = Assertions.EMPTY;

            // resolver of class has priority over the incoming resolver from this function
            var asyncResolver = _resourceResolver ?? resolver.AsAsync();

            // _schemaResolver of class has priority 
            var schemaResolver = new MultiElementSchemaResolver(_schemaResolver, StructureDefinitionToElementSchemaResolver.CreatedCached(asyncResolver));
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


            foreach (var profileUri in getProfiles(instance, profile))
            {
                result += await validate(instance, profileUri);
            }

            outcome.Add(result.ToOperationOutcome());
            return outcome;

            async Task<Assertions> validate(ITypedElement typedElement, string canonicalProfile)
            {
                Assertions assertions = Assertions.EMPTY;
                var schemaUri = new Uri(canonicalProfile, UriKind.RelativeOrAbsolute);
                var schema = await schemaResolver.GetSchema(schemaUri);
                try
                {
                    _stopWatch.Start();
                    assertions += await schema!.Validate(typedElement, validationContext);
                    _stopWatch.Stop();

                }
                catch (Exception ex)
                {
                    assertions += new ResultAssertion(ValidationResult.Failure, new IssueAssertion(-1, "", ex.Message, IssueSeverity.Error));
                }

                return assertions;
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
