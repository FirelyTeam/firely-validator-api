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
using System.Collections.Generic;

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
        /// <param name="schemaResolver">Resolver for schemas that apply for elements.</param>
#pragma warning disable RS0026
        public Validator(
#pragma warning restore RS0026
            IAsyncResourceResolver resourceResolver,
            ICodeValidationTerminologyService terminologyService,
            IExternalReferenceResolver? referenceResolver = null,
            ValidationSettings? settings = null,
            IElementSchemaResolver? schemaResolver = null)
        {
            _settings = settings ?? new ValidationSettings();

            // Set the internal settings that we have hidden in this high-level API.
            if(_settings.ElementSchemaResolver is ValidationSettings.NoopSchemaResolver or null)
#pragma warning disable CS0618 // Type or member is obsolete
                _settings.ElementSchemaResolver = schemaResolver ?? StructureDefinitionToElementSchemaResolver.CreatedCached(resourceResolver);
#pragma warning restore CS0618 // Type or member is obsolete
            if(_settings.ValidateCodeService is ValidationSettings.NoopTerminologyService or null)
                _settings.ValidateCodeService = terminologyService;
            
            _settings.ResolveExternalReference = referenceResolver is not null ? resolve : null;

            PocoNode? resolve(string reference, string location)
            {
                var r = TaskHelper.Await(() => referenceResolver.ResolveAsync(reference));
                return ToPocoNode(r);
            }
        }

        private static PocoNode? ToPocoNode(object? o) =>
            o switch
            {
                null => null,
                ElementNode en => en.ToPocoNode(ModelInfo.ModelInspector),
                Resource r => r.ToPocoNode(ModelInfo.ModelInspector),
                _ => throw new ArgumentException("Reference resolver must return either a Resource or ElementNode.")
            };

        private readonly ValidationSettings _settings;

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameter
        public OperationOutcome Validate(Resource instance, string? profile = null) => Validate(instance.ToPocoNode(ModelInfo.ModelInspector), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public OperationOutcome Validate(ElementNode instance, string? profile = null) => Validate(instance.ToPocoNode(ModelInfo.ModelInspector), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public OperationOutcome Validate(PocoNode sn, string? profile = null)
        {
            profile ??= _settings.TypeNameMapper.MapTypeName(sn.Poco.TypeName).ToString();

#pragma warning disable CS0618 // Type or member is obsolete
            var validator = new SchemaReferenceValidator(profile);
#pragma warning restore CS0618 // Type or member is obsolete
            return validator.Validate(sn, _settings)
                .CleanUp() // cleans up the error outcomes.
                .ToOperationOutcome();
        }
    }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

    /// <summary>
    /// Extension methods to enhance <see cref="ValidationSettings"/>.
    /// </summary>
    public static class ValidationSettingsExtensions
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private static readonly Predicate<IAssertion> FHIRPATHFILTER = ass => ass is FhirPathValidator;
#pragma warning restore CS0618 // Type or member is obsolete

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
