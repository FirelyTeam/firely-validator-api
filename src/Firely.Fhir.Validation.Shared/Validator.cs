/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using System;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A FHIR profile validator, which is able to validate a FHIR resource against a given FHIR profile.
    /// </summary>
    public class Validator
    {
        /// <summary>
        /// Constructs a validator, passing in the services the validator depends on.
        /// </summary>
        /// <param name="resourceResolver">An <see cref="IResourceResolver"/> that is used to resolve the StructureDefinitions to validate against.</param>
        /// <param name="terminologyService">An <see cref="ITerminologyService"/> that is used when the validator must validate a code against a 
        /// terminology service.</param>
        /// <param name="referenceResolver">A <see cref="IExternalReferenceResolver"/> that resolves an url to an external instance, represented as a Model POCO.</param>
        /// <param name="settings">A <see cref="ValidationSettings"/> that contains settings for the validator.</param>
        public Validator(
            IAsyncResourceResolver resourceResolver,
            ICodeValidationTerminologyService terminologyService,
            IExternalReferenceResolver? referenceResolver = null,
            ValidationSettings? settings = null)
        {
            var elementSchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(resourceResolver);

            _settings = settings ?? new ValidationSettings();

            // Set the internal settings that we have hidden in this high-level API.
            _settings.ElementSchemaResolver = elementSchemaResolver;
            _settings.ValidateCodeService = terminologyService;
            _settings.ResolveExternalReference = referenceResolver is not null ? resolve : null;

            ITypedElement? resolve(string reference, string location)
            {
                var r = TaskHelper.Await(() => referenceResolver.ResolveAsync(reference));
                return toTypedElement(r);
            }
        }

        private static ITypedElement? toTypedElement(object? o) =>
            o switch
            {
                null => null,
                ElementNode en => en,
                Resource r => r.ToTypedElement(ModelInfo.ModelInspector),
                _ => throw new ArgumentException("Reference resolver must return either a Resource or ElementNode.")
            };

        private readonly ValidationSettings _settings;

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        // Suppressing this issue since this will be a single method call when we introduce IScopedNode.
        public OperationOutcome Validate(Resource instance, string? profile = null) => Validate(instance.ToTypedElement(ModelInfo.ModelInspector).AsScopedNode(), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public OperationOutcome Validate(ElementNode instance, string? profile = null) => Validate(instance.AsScopedNode(), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        internal OperationOutcome Validate(IScopedNode sn, string? profile = null)
        {
            profile ??= Canonical.ForCoreType(sn.InstanceType).ToString();

            var validator = new SchemaReferenceValidator(profile);
            return validator.Validate(sn, _settings)
                .CleanUp() // cleans up the error outcomes.
                .ToOperationOutcome();
        }
    }

    /// <summary>
    /// Extension methods to enhance <see cref="ValidationSettings"/>.
    /// </summary>
    public static class ValidationSettingsExtensions
    {
        private static readonly Predicate<IAssertion> FHIRPATHFILTER = ass => ass is FhirPathValidator;

        /// <summary>
        /// StructureDefinition may contain FhirPath constraints to enfore invariants in the data that cannot
        /// be expresses using StructureDefinition alone. This validation can be turned off for performance or
        /// debugging purposes.
        /// </summary>
        public static void SetSkipConstraintValidation(this ValidationSettings vc, bool skip)
        {
            if (skip && !vc.ExcludeFilters.Contains(FHIRPATHFILTER))
                vc.ExcludeFilters.Add(FHIRPATHFILTER);
            else if (!skip && vc.ExcludeFilters.Contains(FHIRPATHFILTER))
                vc.ExcludeFilters.Remove(FHIRPATHFILTER);
        }
    }
}
