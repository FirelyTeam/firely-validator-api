using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    internal class DotNetValidator : ITestValidator
    {
        private readonly string[] _unsupportedTests = new[]
        {
            // these tests are not FHIR resources, but CDA resource. We cannot handle at the moment.
            "cda/example", "cda/example-no-styles", "zzz",
            // do not run an Empty testcase
            ValidationManifestDataSourceAttribute.EMPTY_TESTCASE_NAME
        };

        private static readonly IResourceResolver BASE_RESOLVER = new CachedResolver(new StructureDefinitionCorrectionsResolver(ZipSource.CreateValidationSource()));
        private static readonly IElementSchemaResolver SCHEMA_RESOLVER = StructureDefinitionToElementSchemaResolver.CreatedCached(BASE_RESOLVER.AsAsync());
        private readonly Stopwatch _stopWatch;
        private readonly JsonSerializerOptions _serializerOptions;

        public DotNetValidator(Stopwatch? stopwatch = null)
        {
            _stopWatch = stopwatch ?? new();
            _serializerOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector).Pretty();
        }

        public static ITestValidator Create() => new DotNetValidator();

        public string Name => "dotnet-current";

        public string[] UnvalidatableTests => _unsupportedTests;

        public bool CannotValidateTest(TestCase c) => UnvalidatableTests.Contains(c.Name);

        public OperationOutcome? GetExpectedOperationOutcome(IValidatorEnginesResults engine)
        {
            var json = engine.FirelyDotNet?.Outcome;
            if (json is null)
            {
                return null;
            }
            else
            {
                try
                {
                    return JsonSerializer.Deserialize<OperationOutcome>(json.ToJsonString(), _serializerOptions);
                }
                catch (DeserializationFailedException e)
                {
                    return e.PartialResult as OperationOutcome;
                }
            }
        }

        public void SetOperationOutcome(IValidatorEnginesResults engine, OperationOutcome outcome)
        {
            var oo = JsonSerializer.Serialize(outcome, _serializerOptions);
            if (engine.FirelyDotNet is null)
            {
                engine.FirelyDotNet = new ExpectedOutcome() { Outcome = JsonNode.Parse(oo)?.AsObject() };
            }
            else
            {
                engine.FirelyDotNet.Outcome = JsonNode.Parse(oo)?.AsObject();
            }
        }

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
                    var validationSettings = new ValidationSettings(schemaResolver, new LocalTerminologyService(asyncResolver))
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
                    var result = schema!.Validate(typedElement, validationSettings);
                    _stopWatch.Stop();
                    return result;
                }
                catch (FileNotFoundException ex)
                {
                    // Handle file not found errors (e.g., missing FHIR packages)
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-2, $"File was not found: '{ex.Message}'.", IssueSeverity.Error, IssueType.NotFound));
                }
                catch (NotSupportedException ex)
                {
                    // Handle unsupported features or operations
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-3, ex.Message, IssueSeverity.Error, IssueType.NotSupported));
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be resolved"))
                {
                    // Handle profile resolution failures
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-4, ex.Message, IssueSeverity.Error, IssueType.NotFound));
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("loading schema"))
                {
                    // Handle schema loading errors - these are compilation failures that should be surfaced properly
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-5, $"Encountered an error while loading schema '{canonicalProfile}': {ex.Message}", IssueSeverity.Error, IssueType.Exception));
                }
                catch (FormatException ex)
                {
                    // Handle parsing/format errors
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-6, ex.Message, IssueSeverity.Error, IssueType.Invalid));
                }
                catch (ArgumentException ex)
                {
                    // Handle invalid arguments (e.g., malformed URIs, invalid configurations)
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-7, ex.Message, IssueSeverity.Error, IssueType.Invalid));
                }
                catch (Exception ex)
                {
                    // For unexpected exceptions that we haven't categorized yet, still use -1 but with more context
                    var errorMessage = $"Unexpected error during validation of profile '{canonicalProfile}': {ex.GetType().Name}: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" (Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message})";
                    }
                    return new ResultReport(ValidationResult.Failure, new IssueAssertion(-1, errorMessage, IssueSeverity.Error));
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

                var instanceType = node.InstanceType is not null ? ModelInfo.CanonicalUriForFhirCoreType(node.InstanceType) : null;
                if (instanceType is not null)
                {
                    yield return instanceType!;
                }
            }
        }
    }
}
