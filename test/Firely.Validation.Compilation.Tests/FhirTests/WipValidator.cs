using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class WipValidator : ITestValidator
    {
        private readonly List<string> _ignoreTestList = new();
        private readonly IElementSchemaResolver _schemaResolver;

        public WipValidator(IElementSchemaResolver schemaResolver)
        {
            _schemaResolver = schemaResolver;
        }

        public ExpectedResult? GetExpectedResults(IValidatorEngines engine) => engine.FirelySDKWip;
        public void SetExpectedResults(IValidatorEngines engine, ExpectedResult result) => engine.FirelySDKWip = result;
        public bool ShouldIgnoreTest(string name) => _ignoreTestList.Contains(name);

        /// <summary>
        /// Validator engine based in this solution: the work in progress (wip) validator
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public OperationOutcome Validate(ITypedElement instance, IResourceResolver resolver, string? profile = null)
        {
            var outcome = new OperationOutcome();
            var result = Assertions.EMPTY;
            var asyncResolver = resolver.AsAsync();

            foreach (var profileUri in getProfiles(instance, profile))
            {
                result += validate(instance, profileUri);
            }

            outcome.Add(result.ToOperationOutcome());
            return outcome;

            Assertions validate(ITypedElement typedElement, string canonicalProfile)
            {
                Assertions assertions = Assertions.EMPTY;
                var schemaUri = new Uri(canonicalProfile, UriKind.RelativeOrAbsolute);
                try
                {
                    var schemaResolver = new MultiElementSchemaResolver(_schemaResolver, StructureDefinitionToElementSchemaResolver.CreatedCached(asyncResolver));
                    var schema = TaskHelper.Await(() => schemaResolver.GetSchema(schemaUri));
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
                    assertions += TaskHelper.Await(() => schema!.Validate(typedElement, validationContext));
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
